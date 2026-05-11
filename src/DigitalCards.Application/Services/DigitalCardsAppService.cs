using DigitalCards.Application.Abstractions;
using DigitalCards.Application.Models;
using DigitalCards.Domain;

namespace DigitalCards.Application.Services;

public sealed class DigitalCardsAppService
{
    private readonly IBusinessRepository _businesses;
    private readonly IClientRepository _clients;
    private readonly IClock _clock;
    private readonly IEmailSender _emailSender;
    private readonly IGoogleWalletService _googleWallet;
    private readonly IAppleWalletService _appleWallet;
    private readonly ILoyaltyCardRepository _loyaltyCards;

    public DigitalCardsAppService(
        IClientRepository clients,
        IBusinessRepository businesses,
        ILoyaltyCardRepository loyaltyCards,
        IGoogleWalletService googleWallet,
        IAppleWalletService appleWallet,
        IEmailSender emailSender,
        IClock clock)
    {
        _clients = clients;
        _businesses = businesses;
        _loyaltyCards = loyaltyCards;
        _googleWallet = googleWallet;
        _appleWallet = appleWallet;
        _emailSender = emailSender;
        _clock = clock;
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
        if (business is null || !LegacyPasswordVerifier.Matches(business.PasswordHashPlaceholder, command.Password))
        {
            return null;
        }

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

        var enrollmentUrl = $"{command.BaseUrl.TrimEnd('/')}/Wallet/Select/{card.EnrollmentToken}";
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
            card.EnrollmentToken,
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
            DownloadUrl = $"{baseUrl.TrimEnd('/')}/Wallet/Apple/Download/{card.EnrollmentToken}.pkpass"
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
        var card = await _loyaltyCards.FindByEnrollmentTokenAsync(token, cancellationToken);
        if (card is null)
        {
            return null;
        }

        var client = await _clients.FindByIdAsync(card.ClientId, cancellationToken);
        var business = await _businesses.FindByIdAsync(card.BusinessId, cancellationToken);

        return client is null || business is null ? null : (card, client, business);
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
}
