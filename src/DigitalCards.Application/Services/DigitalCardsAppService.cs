using DigitalCards.Application.Abstractions;
using DigitalCards.Application.Models;
using DigitalCards.Domain;
using Microsoft.AspNetCore.Identity;

namespace DigitalCards.Application.Services;

public sealed class DigitalCardsAppService
{
    private readonly IBusinessCredentialRepository _businessCredentials;
    private readonly IBusinessRepository _businesses;
    private readonly IClientRepository _clients;
    private readonly IClock _clock;
    private readonly IEmailSender _emailSender;
    private readonly IGoogleWalletService _googleWallet;
    private readonly IAppleWalletService _appleWallet;
    private readonly IAppleWalletPassRepository _appleWalletPasses;
    private readonly ILoyaltyCardRepository _loyaltyCards;
    private readonly IPasswordHasher<BusinessPasswordHashSubject> _passwordHasher;
    private readonly IWalletLinkTokenService _walletLinkTokens;

    public DigitalCardsAppService(
        IClientRepository clients,
        IBusinessRepository businesses,
        IBusinessCredentialRepository businessCredentials,
        ILoyaltyCardRepository loyaltyCards,
        IGoogleWalletService googleWallet,
        IAppleWalletService appleWallet,
        IAppleWalletPassRepository appleWalletPasses,
        IEmailSender emailSender,
        IClock clock,
        IPasswordHasher<BusinessPasswordHashSubject> passwordHasher,
        IWalletLinkTokenService walletLinkTokens)
    {
        _clients = clients;
        _businesses = businesses;
        _businessCredentials = businessCredentials;
        _loyaltyCards = loyaltyCards;
        _googleWallet = googleWallet;
        _appleWallet = appleWallet;
        _appleWalletPasses = appleWalletPasses;
        _emailSender = emailSender;
        _clock = clock;
        _passwordHasher = passwordHasher;
        _walletLinkTokens = walletLinkTokens;
    }

    public async Task<ClientDto> RegisterClientAsync(RegisterClientCommand command, CancellationToken cancellationToken = default)
    {
        var existing = await _clients.FindByUserNameOrEmailAsync(command.UserName, cancellationToken)
            ?? await _clients.FindByUserNameOrEmailAsync(command.Email, cancellationToken);

        if (existing is not null)
        {
            throw new InvalidOperationException("Client username or email already exists.");
        }

        var client = new Client(Guid.NewGuid(), command.UserName, command.FirstName, command.LastName, command.Email);
        await _clients.AddAsync(client, cancellationToken);
        return ToDto(client);
    }

    public async Task<BusinessDto?> LoginBusinessAsync(BusinessLoginCommand command, CancellationToken cancellationToken = default)
    {
        var business = await _businesses.FindByEmailAsync(command.Email, cancellationToken);
        if (business is null)
        {
            return null;
        }

        var subject = new BusinessPasswordHashSubject(business.Id);
        var credential = await _businessCredentials.FindByBusinessIdAsync(business.Id, cancellationToken);
        if (credential is not null)
        {
            var verification = _passwordHasher.VerifyHashedPassword(
                subject,
                credential.PasswordHash,
                command.Password);

            if (verification == PasswordVerificationResult.Failed)
            {
                return null;
            }

            if (verification == PasswordVerificationResult.SuccessRehashNeeded)
            {
                await _businessCredentials.UpsertAsync(
                    credential.Rehash(_passwordHasher.HashPassword(subject, command.Password), _clock.UtcNow),
                    cancellationToken);
            }

            return ToDto(business);
        }

        if (!LegacyPasswordVerifier.Matches(business.PasswordHashPlaceholder, command.Password))
        {
            return null;
        }

        var now = _clock.UtcNow;
        await _businessCredentials.UpsertAsync(
            new BusinessCredential(
                business.Id,
                _passwordHasher.HashPassword(subject, command.Password),
                now,
                now),
            cancellationToken);

        return ToDto(business);
    }

    public async Task<EnrollClientResult> EnrollClientAsync(EnrollClientCommand command, CancellationToken cancellationToken = default)
    {
        var business = await RequireBusinessAsync(command.BusinessId, cancellationToken);
        var client = await RequireClientAsync(command.UserNameOrEmail, cancellationToken);

        var card = await _loyaltyCards.FindByClientAndBusinessAsync(client.Id, business.Id, cancellationToken);
        if (card is null)
        {
            card = new LoyaltyCard(Guid.NewGuid(), client.Id, business.Id, _clock.UtcNow);
            card = await _loyaltyCards.AddAsync(card, cancellationToken);
        }

        var publicToken = await _walletLinkTokens.CreateTokenAsync(
            card.Id,
            WalletLinkPurposes.WalletSelect,
            cancellationToken);
        var enrollmentUrl = $"{command.BaseUrl.TrimEnd('/')}/Wallet/Select/{publicToken}";
        await _emailSender.SendWalletEnrollmentAsync(
            new WalletEnrollmentEmail(client.Email, client.FullName, business.Name, enrollmentUrl, _clock.UtcNow),
            cancellationToken);

        return new EnrollClientResult(ToDto(card, client, business), enrollmentUrl);
    }

    public async Task<WalletLandingDto?> GetWalletLandingAsync(string token, CancellationToken cancellationToken = default)
    {
        var context = await FindCardContextByTokenAsync(token, cancellationToken);
        if (context is null)
        {
            return null;
        }

        var (card, client, business) = context.Value;
        return new WalletLandingDto(
            token,
            business.Name,
            client.FullName,
            card.CurrentStamps,
            card.LifetimeStamps,
            card.GoogleObjectId is not null);
    }

    public async Task<GoogleWalletIssueResult?> SelectGoogleWalletAsync(string token, CancellationToken cancellationToken = default)
    {
        var context = await FindCardContextByTokenAsync(token, cancellationToken);
        if (context is null)
        {
            return null;
        }

        var (card, client, business) = context.Value;
        if (card.GoogleObjectId is not null && card.GoogleSaveUrl is not null)
        {
            return new GoogleWalletIssueResult(card.GoogleObjectId, card.GoogleSaveUrl);
        }

        var result = await _googleWallet.IssueSaveLinkAsync(card, client, business, cancellationToken);
        card.MarkGoogleIssued(result.ObjectId, result.SaveUrl);
        await _loyaltyCards.UpdateAsync(card, cancellationToken);
        return result;
    }

    public Task<AppleWalletIssueResult?> SelectAppleWalletAsync(string token, CancellationToken cancellationToken = default)
    {
        return SelectAppleWalletAsync(token, baseUrl: null, cancellationToken);
    }

    public async Task<AppleWalletIssueResult?> SelectAppleWalletAsync(
        string token,
        string? baseUrl,
        CancellationToken cancellationToken = default)
    {
        var context = await FindCardContextByTokenAsync(token, cancellationToken);
        if (context is null)
        {
            return null;
        }

        var (card, client, business) = context.Value;
        var result = await _appleWallet.IssueAsync(card, client, business, cancellationToken);
        if (result.Status != AppleWalletIssueStatus.Ready ||
            !string.IsNullOrWhiteSpace(result.DownloadUrl) ||
            string.IsNullOrWhiteSpace(baseUrl))
        {
            return result;
        }

        return result with
        {
            DownloadUrl = $"{baseUrl.TrimEnd('/')}/Wallet/Apple/Download/{token}.pkpass"
        };
    }

    public async Task<AppleWalletPassFile?> DownloadAppleWalletPassAsync(
        string token,
        CancellationToken cancellationToken = default)
    {
        var context = await FindCardContextByTokenAsync(token, cancellationToken);
        if (context is null)
        {
            return null;
        }

        var (card, client, business) = context.Value;
        return await _appleWallet.CreatePassAsync(card, client, business, cancellationToken);
    }

    public async Task<LoyaltyCardDto> AddStampAsync(AddStampCommand command, CancellationToken cancellationToken = default)
    {
        var business = await RequireBusinessAsync(command.BusinessId, cancellationToken);
        var client = await RequireClientAsync(command.UserNameOrEmail, cancellationToken);
        var card = await _loyaltyCards.FindByClientAndBusinessAsync(client.Id, business.Id, cancellationToken);

        if (card is null)
        {
            throw new InvalidOperationException("Client is not enrolled with this business.");
        }

        card.AddStamp(_clock.UtcNow);
        await _loyaltyCards.UpdateAsync(card, cancellationToken);

        if (card.GoogleObjectId is not null)
        {
            await _googleWallet.PatchStampStateAsync(card, client, business, cancellationToken);
        }

        await _appleWallet.NotifyPassUpdatedAsync(card, client, business, cancellationToken);

        return ToDto(card, client, business);
    }

    public async Task<IReadOnlyList<BusinessCardDto>> SearchBusinessCardsAsync(
        Guid businessId,
        string query,
        CancellationToken cancellationToken = default)
    {
        var business = await RequireBusinessAsync(businessId, cancellationToken);
        var cards = await _loyaltyCards.SearchByBusinessAsync(business.Id, query, limit: 25, cancellationToken);
        var results = new List<BusinessCardDto>(cards.Count);

        foreach (var card in cards)
        {
            var client = await _clients.FindByIdAsync(card.ClientId, cancellationToken);
            if (client is null)
            {
                continue;
            }

            results.Add(await ToBusinessCardDtoAsync(card, client, business, cancellationToken));
        }

        return results;
    }

    public async Task<BusinessCardDto?> GetBusinessCardDetailAsync(
        Guid businessId,
        Guid cardId,
        CancellationToken cancellationToken = default)
    {
        var context = await FindBusinessCardContextAsync(businessId, cardId, cancellationToken);
        if (context is null)
        {
            return null;
        }

        var (card, client, business) = context.Value;
        return await ToBusinessCardDtoAsync(card, client, business, cancellationToken);
    }

    public async Task<BusinessCardDto?> AddStampToCardAsync(
        Guid businessId,
        Guid cardId,
        CancellationToken cancellationToken = default)
    {
        var context = await FindBusinessCardContextAsync(businessId, cardId, cancellationToken);
        if (context is null)
        {
            return null;
        }

        var (card, client, business) = context.Value;
        card.AddStamp(_clock.UtcNow);
        await _loyaltyCards.UpdateAsync(card, cancellationToken);

        if (card.GoogleObjectId is not null)
        {
            await _googleWallet.PatchStampStateAsync(card, client, business, cancellationToken);
        }

        await _appleWallet.NotifyPassUpdatedAsync(card, client, business, cancellationToken);
        return await ToBusinessCardDtoAsync(card, client, business, cancellationToken);
    }

    public async Task<ResendWalletEmailResult?> ResendWalletEmailAsync(
        Guid businessId,
        Guid cardId,
        string baseUrl,
        CancellationToken cancellationToken = default)
    {
        var context = await FindBusinessCardContextAsync(businessId, cardId, cancellationToken);
        if (context is null)
        {
            return null;
        }

        var (card, client, business) = context.Value;
        var publicToken = await _walletLinkTokens.CreateTokenAsync(
            card.Id,
            WalletLinkPurposes.WalletSelect,
            cancellationToken);
        var enrollmentUrl = $"{baseUrl.TrimEnd('/')}/Wallet/Select/{publicToken}";
        await _emailSender.SendWalletEnrollmentAsync(
            new WalletEnrollmentEmail(client.Email, client.FullName, business.Name, enrollmentUrl, _clock.UtcNow),
            cancellationToken);

        return new ResendWalletEmailResult(
            await ToBusinessCardDtoAsync(card, client, business, cancellationToken),
            enrollmentUrl);
    }

    public async Task<IReadOnlyList<LoyaltyCardDto>> GetClientCardsAsync(string userNameOrEmail, CancellationToken cancellationToken = default)
    {
        var client = await RequireClientAsync(userNameOrEmail, cancellationToken);
        var cards = await _loyaltyCards.ListByClientAsync(client.Id, cancellationToken);
        var businesses = await _businesses.ListAsync(cancellationToken);

        return cards
            .Select(card => ToDto(card, client, businesses.Single(business => business.Id == card.BusinessId)))
            .ToArray();
    }

    private async Task<Business> RequireBusinessAsync(Guid businessId, CancellationToken cancellationToken)
    {
        return await _businesses.FindByIdAsync(businessId, cancellationToken)
            ?? throw new InvalidOperationException("Business was not found.");
    }

    private async Task<Client> RequireClientAsync(string userNameOrEmail, CancellationToken cancellationToken)
    {
        return await _clients.FindByUserNameOrEmailAsync(userNameOrEmail, cancellationToken)
            ?? throw new InvalidOperationException("Client was not found.");
    }

    private async Task<(LoyaltyCard Card, Client Client, Business Business)?> FindCardContextByTokenAsync(
        string token,
        CancellationToken cancellationToken)
    {
        LoyaltyCard? card = null;
        var cardId = await _walletLinkTokens.ResolveCardIdAsync(
            token,
            WalletLinkPurposes.WalletSelect,
            cancellationToken);

        if (cardId is not null)
        {
            card = await _loyaltyCards.FindByIdAsync(cardId.Value, cancellationToken);
        }
        else if (_walletLinkTokens.AllowLegacyCardIdTokens)
        {
            card = await _loyaltyCards.FindByEnrollmentTokenAsync(token, cancellationToken);
        }

        if (card is null)
        {
            return null;
        }

        var client = await _clients.FindByIdAsync(card.ClientId, cancellationToken);
        var business = await _businesses.FindByIdAsync(card.BusinessId, cancellationToken);

        return client is null || business is null ? null : (card, client, business);
    }

    private async Task<(LoyaltyCard Card, Client Client, Business Business)?> FindBusinessCardContextAsync(
        Guid businessId,
        Guid cardId,
        CancellationToken cancellationToken)
    {
        var business = await _businesses.FindByIdAsync(businessId, cancellationToken);
        var card = await _loyaltyCards.FindByIdAsync(cardId, cancellationToken);
        if (business is null || card is null || card.BusinessId != business.Id)
        {
            return null;
        }

        var client = await _clients.FindByIdAsync(card.ClientId, cancellationToken);
        return client is null ? null : (card, client, business);
    }

    private static ClientDto ToDto(Client client)
    {
        return new ClientDto(client.Id, client.UserName, client.FirstName, client.LastName, client.Email);
    }

    private static BusinessDto ToDto(Business business)
    {
        return new BusinessDto(business.Id, business.Name, business.Email, business.LogoPath);
    }

    private static LoyaltyCardDto ToDto(LoyaltyCard card, Client client, Business business)
    {
        return new LoyaltyCardDto(
            card.Id,
            card.EnrollmentToken,
            business.Name,
            client.UserName,
            card.CurrentStamps,
            card.LifetimeStamps,
            card.GoogleObjectId,
            card.GoogleSaveUrl);
    }

    private async Task<BusinessCardDto> ToBusinessCardDtoAsync(
        LoyaltyCard card,
        Client client,
        Business business,
        CancellationToken cancellationToken)
    {
        var applePass = await _appleWalletPasses.FindPassByCardIdAsync(card.Id, cancellationToken);
        var appleDeviceCount = 0;
        if (applePass is not null)
        {
            var devices = await _appleWalletPasses.ListDevicesForPassAsync(
                applePass.PassTypeIdentifier,
                applePass.SerialNumber,
                cancellationToken);
            appleDeviceCount = devices.Count;
        }

        return new BusinessCardDto(
            card.Id,
            card.EnrollmentToken,
            ToDto(client),
            business.Name,
            card.CurrentStamps,
            card.LifetimeStamps,
            card.LastStampedAt,
            !string.IsNullOrWhiteSpace(card.GoogleObjectId),
            applePass is not null,
            appleDeviceCount,
            applePass?.UpdatedAt);
    }
}
