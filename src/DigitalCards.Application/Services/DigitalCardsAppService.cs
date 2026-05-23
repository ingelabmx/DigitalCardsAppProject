using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using DigitalCards.Application.Abstractions;
using DigitalCards.Application.Models;
using DigitalCards.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace DigitalCards.Application.Services;

public sealed class DigitalCardsAppService
{
    private const int BrandingNameMaxLength = 80;
    private const int BrandingLogoMaxLength = 200;
    private const int BrandingDescriptionMaxLength = 280;
    private const int ClientNameMaxLength = 30;
    private const int ClientEmailMaxLength = 30;
    private const string DefaultPrimaryColor = "#111827";
    private const string DefaultSecondaryColor = "#2563eb";
    private const string DefaultCustomFieldColor = "#FFFFFF";
    private const string DefaultProgramName = "Tarjeta de lealtad";
    private const string DefaultProgramDescription = "Acumula sellos digitales y consulta tu tarjeta en Wallet.";
    private const string RewardRedemptionTableMissingMessage =
        "No se puede canjear la recompensa en este momento. Contacta a soporte.";
    private const int MaxStampGoal = 1000;

    private readonly IBusinessCredentialRepository _businessCredentials;
    private readonly IBusinessBrandingRepository _businessBranding;
    private readonly IBusinessRepository _businesses;
    private readonly IClientConsentRepository _clientConsents;
    private readonly IClientCredentialRepository _clientCredentials;
    private readonly IClientRepository _clients;
    private readonly IClock _clock;
    private readonly IEmailSender _emailSender;
    private readonly IGoogleWalletService _googleWallet;
    private readonly IAppleWalletService _appleWallet;
    private readonly IAppleWalletPassRepository _appleWalletPasses;
    private readonly ILogger<DigitalCardsAppService> _logger;
    private readonly IAccountLifecycleRepository _accountLifecycle;
    private readonly ILoyaltyCardRepository _loyaltyCards;
    private readonly IPasswordResetTokenRepository _passwordResetTokens;
    private readonly IPasswordHasher<BusinessPasswordHashSubject> _passwordHasher;
    private readonly IPasswordHasher<ClientPasswordHashSubject> _clientPasswordHasher;
    private readonly IRewardRedemptionRepository _rewardRedemptions;
    private readonly IStampLedgerRepository _stampLedger;
    private readonly WalletBrandingRefreshService _walletBrandingRefresh;
    private readonly IWalletLinkTokenService _walletLinkTokens;

    public DigitalCardsAppService(
        IClientRepository clients,
        IClientCredentialRepository clientCredentials,
        IClientConsentRepository clientConsents,
        IBusinessRepository businesses,
        IBusinessBrandingRepository businessBranding,
        IBusinessCredentialRepository businessCredentials,
        ILoyaltyCardRepository loyaltyCards,
        IGoogleWalletService googleWallet,
        IAppleWalletService appleWallet,
        IAppleWalletPassRepository appleWalletPasses,
        IAccountLifecycleRepository accountLifecycle,
        IEmailSender emailSender,
        IClock clock,
        IPasswordHasher<BusinessPasswordHashSubject> passwordHasher,
        IPasswordHasher<ClientPasswordHashSubject> clientPasswordHasher,
        IRewardRedemptionRepository rewardRedemptions,
        IStampLedgerRepository stampLedger,
        WalletBrandingRefreshService walletBrandingRefresh,
        IWalletLinkTokenService walletLinkTokens,
        IPasswordResetTokenRepository passwordResetTokens,
        ILogger<DigitalCardsAppService> logger)
    {
        _clients = clients;
        _clientCredentials = clientCredentials;
        _clientConsents = clientConsents;
        _businesses = businesses;
        _businessBranding = businessBranding;
        _businessCredentials = businessCredentials;
        _loyaltyCards = loyaltyCards;
        _passwordResetTokens = passwordResetTokens;
        _googleWallet = googleWallet;
        _appleWallet = appleWallet;
        _appleWalletPasses = appleWalletPasses;
        _accountLifecycle = accountLifecycle;
        _emailSender = emailSender;
        _clock = clock;
        _passwordHasher = passwordHasher;
        _clientPasswordHasher = clientPasswordHasher;
        _rewardRedemptions = rewardRedemptions;
        _stampLedger = stampLedger;
        _walletBrandingRefresh = walletBrandingRefresh;
        _walletLinkTokens = walletLinkTokens;
        _logger = logger;
    }

    public async Task<ClientDto> RegisterClientAsync(RegisterClientCommand command, CancellationToken cancellationToken = default)
    {
        var userName = ClientUserNameNormalizer.NormalizeUserName(command.UserName);
        if (!ClientUserNameNormalizer.IsValidUserName(command.UserName))
        {
            throw new InvalidOperationException("El usuario solo puede usar letras y numeros, sin espacios.");
        }

        var email = command.Email.Trim().ToLowerInvariant();
        if (await _clients.UserNameOrEmailExistsAsync(userName, cancellationToken) ||
            await _clients.UserNameOrEmailExistsAsync(email, cancellationToken))
        {
            throw new InvalidOperationException("Client username or email already exists.");
        }

        var legacyPasswordHash = string.IsNullOrWhiteSpace(command.Password)
            ? string.Empty
            : LegacyPasswordVerifier.CreateLegacyBusinessPasswordHash(command.Password);
        var client = new Client(
            Guid.NewGuid(),
            userName,
            command.FirstName,
            command.LastName,
            email,
            legacyPasswordHash);
        await _clients.AddAsync(client, cancellationToken);
        if (!string.IsNullOrWhiteSpace(command.Password))
        {
            var now = _clock.UtcNow;
            var subject = new ClientPasswordHashSubject(client.Id);
            await _clientCredentials.UpsertAsync(
                new ClientCredential(
                    client.Id,
                    _clientPasswordHasher.HashPassword(subject, command.Password),
                    now,
                    now),
                cancellationToken);
        }

        return ToDto(client);
    }

    public async Task RecordClientConsentAsync(
        RecordClientConsentCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command.ClientId == Guid.Empty)
        {
            throw new InvalidOperationException("Client is required for consent.");
        }

        var client = await _clients.FindByIdAsync(command.ClientId, cancellationToken);
        if (client is null)
        {
            throw new InvalidOperationException("Client does not exist.");
        }

        if (command.BusinessId is not null &&
            await _businesses.FindByIdAsync(command.BusinessId.Value, cancellationToken) is null)
        {
            throw new InvalidOperationException("Business does not exist.");
        }

        await _clientConsents.AddAsync(
            new ClientConsent(
                0,
                command.ClientId,
                command.BusinessId,
                NormalizeConsentValue(command.PolicyVersion, "privacy-2026-05"),
                NormalizeConsentValue(command.Source, "Unknown"),
                _clock.UtcNow),
            cancellationToken);
    }

    public async Task<ClientDto?> LoginClientAsync(ClientLoginCommand command, CancellationToken cancellationToken = default)
    {
        var client = await _clients.FindByUserNameOrEmailAsync(
            ClientUserNameNormalizer.NormalizeUserNameOrEmail(command.UserNameOrEmail),
            cancellationToken);
        if (client is null)
        {
            return null;
        }

        var subject = new ClientPasswordHashSubject(client.Id);
        var credential = await _clientCredentials.FindByClientIdAsync(client.Id, cancellationToken);
        if (credential is not null)
        {
            var verification = _clientPasswordHasher.VerifyHashedPassword(
                subject,
                credential.PasswordHash,
                command.Password);

            if (verification == PasswordVerificationResult.Failed)
            {
                return null;
            }

            if (verification == PasswordVerificationResult.SuccessRehashNeeded)
            {
                await _clientCredentials.UpsertAsync(
                    credential.Rehash(_clientPasswordHasher.HashPassword(subject, command.Password), _clock.UtcNow),
                    cancellationToken);
            }

            return ToDto(client);
        }

        if (string.IsNullOrWhiteSpace(client.PasswordHashPlaceholder) ||
            !LegacyPasswordVerifier.Matches(client.PasswordHashPlaceholder, command.Password))
        {
            return null;
        }

        var now = _clock.UtcNow;
        await _clientCredentials.UpsertAsync(
            new ClientCredential(
                client.Id,
                _clientPasswordHasher.HashPassword(subject, command.Password),
                now,
                now),
            cancellationToken);

        return ToDto(client);
    }

    public async Task<ChangeClientPasswordResult> ChangeClientPasswordAsync(
        ChangeClientPasswordCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command.ClientId == Guid.Empty)
        {
            return new ChangeClientPasswordResult(false, "La sesion de cliente no es valida.");
        }

        if (string.IsNullOrWhiteSpace(command.CurrentPassword))
        {
            return new ChangeClientPasswordResult(false, "La contrasena actual es requerida.");
        }

        if (string.IsNullOrWhiteSpace(command.NewPassword))
        {
            return new ChangeClientPasswordResult(false, "La contrasena nueva es requerida.");
        }

        if (command.NewPassword.Length < 8)
        {
            return new ChangeClientPasswordResult(false, "La contrasena nueva debe tener al menos 8 caracteres.");
        }

        if (command.NewPassword.Length > 128)
        {
            return new ChangeClientPasswordResult(false, "La contrasena nueva no puede exceder 128 caracteres.");
        }

        var client = await _clients.FindByIdAsync(command.ClientId, cancellationToken);
        if (client is null)
        {
            return new ChangeClientPasswordResult(false, "El cliente no existe.");
        }

        if (!await VerifyClientPasswordAsync(client, command.CurrentPassword, migrateLegacy: false, cancellationToken))
        {
            return new ChangeClientPasswordResult(false, "La contrasena actual no es valida.");
        }

        var legacyPasswordHash = LegacyPasswordVerifier.CreateLegacyBusinessPasswordHash(command.NewPassword);
        client = await _clients.UpdatePasswordAsync(client.Id, legacyPasswordHash, cancellationToken);

        var now = _clock.UtcNow;
        var subject = new ClientPasswordHashSubject(client.Id);
        await _clientCredentials.UpsertAsync(
            new ClientCredential(
                client.Id,
                _clientPasswordHasher.HashPassword(subject, command.NewPassword),
                now,
                now),
            cancellationToken);

        return new ChangeClientPasswordResult(true, ErrorMessage: null);
    }

    public async Task<ClientDto?> GetClientProfileAsync(
        Guid clientId,
        CancellationToken cancellationToken = default)
    {
        var client = await _clients.FindByIdAsync(clientId, cancellationToken);
        return client is null ? null : ToDto(client);
    }

    public async Task<UpdateClientProfileResult> UpdateClientProfileAsync(
        UpdateClientProfileCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command.ClientId == Guid.Empty)
        {
            return new UpdateClientProfileResult(null, "La sesion de cliente no es valida.");
        }

        var firstName = command.FirstName.Trim();
        var lastName = command.LastName.Trim();
        var email = command.Email.Trim().ToLowerInvariant();
        var validationError = ValidateClientProfile(firstName, lastName, email);
        if (validationError is not null)
        {
            return new UpdateClientProfileResult(null, validationError);
        }

        var existing = await _clients.FindByIdAsync(command.ClientId, cancellationToken);
        if (existing is null)
        {
            return new UpdateClientProfileResult(null, "El cliente no existe.");
        }

        if (!string.Equals(existing.Email, email, StringComparison.OrdinalIgnoreCase) &&
            await _clients.EmailExistsForOtherUserAsync(existing.Id, email, cancellationToken))
        {
            return new UpdateClientProfileResult(null, "El correo ya esta registrado.");
        }

        try
        {
            var updated = await _clients.UpdateProfileAsync(
                existing.Id,
                firstName,
                lastName,
                email,
                cancellationToken);
            return new UpdateClientProfileResult(ToDto(updated), ErrorMessage: null);
        }
        catch (InvalidOperationException)
        {
            return new UpdateClientProfileResult(null, "No se pudo actualizar el perfil.");
        }
    }

    public async Task<PasswordResetRequestResult> RequestClientPasswordResetAsync(
        RequestClientPasswordResetCommand command,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.UserNameOrEmail))
        {
            return new PasswordResetRequestResult(Accepted: true);
        }

        var client = await _clients.FindByUserNameOrEmailAsync(
            ClientUserNameNormalizer.NormalizeUserNameOrEmail(command.UserNameOrEmail),
            cancellationToken);
        if (client is null)
        {
            return new PasswordResetRequestResult(Accepted: true);
        }

        var token = await CreatePasswordResetTokenAsync(
            PasswordResetAccountType.Client,
            client.Id,
            cancellationToken);
        await _emailSender.SendPasswordResetAsync(
            new PasswordResetEmail(
                client.Email,
                client.FullName,
                "cliente",
                $"{command.BaseUrl.TrimEnd('/')}/Client/ResetPassword/{token.Token}",
                token.ExpiresAt,
                _clock.UtcNow),
            cancellationToken);

        return new PasswordResetRequestResult(Accepted: true);
    }

    public async Task<PasswordResetRequestResult> RequestBusinessPasswordResetAsync(
        RequestBusinessPasswordResetCommand command,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.Email))
        {
            return new PasswordResetRequestResult(Accepted: true);
        }

        var business = await _businesses.FindByEmailAsync(command.Email, cancellationToken);
        if (business is null)
        {
            return new PasswordResetRequestResult(Accepted: true);
        }

        var displayBusiness = await ApplyBrandingAsync(business, cancellationToken);
        var token = await CreatePasswordResetTokenAsync(
            PasswordResetAccountType.Business,
            business.Id,
            cancellationToken);
        await _emailSender.SendPasswordResetAsync(
            new PasswordResetEmail(
                business.Email,
                displayBusiness.DisplayName,
                "negocio",
                $"{command.BaseUrl.TrimEnd('/')}/Business/ResetPassword/{token.Token}",
                token.ExpiresAt,
                _clock.UtcNow,
                new EmailBranding(
                    displayBusiness.DisplayName,
                    BuildPublicAssetUrl(displayBusiness.LogoPath, command.BaseUrl),
                    displayBusiness.PrimaryColor,
                    displayBusiness.ProgramName)),
            cancellationToken);

        return new PasswordResetRequestResult(Accepted: true);
    }

    public async Task<ResetPasswordResult> ResetClientPasswordAsync(
        ResetPasswordCommand command,
        CancellationToken cancellationToken = default)
    {
        var validationError = ValidateResetPasswordCommand(command);
        if (validationError is not null)
        {
            return new ResetPasswordResult(false, validationError);
        }

        var token = await FindPasswordResetTokenAsync(
            command.Token,
            PasswordResetAccountType.Client,
            cancellationToken);
        if (token is null)
        {
            return new ResetPasswordResult(false, "El link no es valido o ya expiro.");
        }

        var client = await _clients.FindByIdAsync(token.AccountId, cancellationToken);
        if (client is null)
        {
            return new ResetPasswordResult(false, "El link no es valido o ya expiro.");
        }

        var legacyPasswordHash = LegacyPasswordVerifier.CreateLegacyBusinessPasswordHash(command.NewPassword);
        client = await _clients.UpdatePasswordAsync(client.Id, legacyPasswordHash, cancellationToken);

        var now = _clock.UtcNow;
        var subject = new ClientPasswordHashSubject(client.Id);
        await _clientCredentials.UpsertAsync(
            new ClientCredential(
                client.Id,
                _clientPasswordHasher.HashPassword(subject, command.NewPassword),
                now,
                now),
            cancellationToken);
        await _passwordResetTokens.MarkUsedAsync(token.Id, now, cancellationToken);

        return new ResetPasswordResult(true, ErrorMessage: null);
    }

    public async Task<ResetPasswordResult> ResetBusinessPasswordAsync(
        ResetPasswordCommand command,
        CancellationToken cancellationToken = default)
    {
        var validationError = ValidateResetPasswordCommand(command);
        if (validationError is not null)
        {
            return new ResetPasswordResult(false, validationError);
        }

        var token = await FindPasswordResetTokenAsync(
            command.Token,
            PasswordResetAccountType.Business,
            cancellationToken);
        if (token is null)
        {
            return new ResetPasswordResult(false, "El link no es valido o ya expiro.");
        }

        var business = await _businesses.FindByIdAsync(token.AccountId, cancellationToken);
        if (business is null)
        {
            return new ResetPasswordResult(false, "El link no es valido o ya expiro.");
        }

        var legacyPasswordHash = LegacyPasswordVerifier.CreateLegacyBusinessPasswordHash(command.NewPassword);
        business = await _businesses.UpdateAsync(
            new Business(
                business.Id,
                business.Name,
                business.Email,
                legacyPasswordHash,
                business.LogoPath,
                business.PublicName,
                business.PrimaryColor,
                business.SecondaryColor,
                business.ProgramName,
                business.ProgramDescription,
                business.CustomFieldColor),
            cancellationToken);

        var now = _clock.UtcNow;
        var subject = new BusinessPasswordHashSubject(business.Id);
        await _businessCredentials.UpsertAsync(
            new BusinessCredential(
                business.Id,
                _passwordHasher.HashPassword(subject, command.NewPassword),
                now,
                now),
            cancellationToken);
        await _passwordResetTokens.MarkUsedAsync(token.Id, now, cancellationToken);

        return new ResetPasswordResult(true, ErrorMessage: null);
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

    public async Task<BusinessBrandingSettingsDto?> GetBusinessBrandingSettingsAsync(
        Guid businessId,
        CancellationToken cancellationToken = default)
    {
        var business = await _businesses.FindByIdAsync(businessId, cancellationToken);
        return business is null ? null : await ToBusinessBrandingSettingsAsync(business, cancellationToken);
    }

    public async Task<BusinessDto?> GetBusinessShellAsync(
        Guid businessId,
        CancellationToken cancellationToken = default)
    {
        var business = await _businesses.FindByIdAsync(businessId, cancellationToken);
        return business is null
            ? null
            : ToDto(await ApplyBrandingAsync(business, cancellationToken));
    }

    public async Task<BusinessSelfServiceBrandingResult> UpdateBusinessBrandingAsync(
        UpdateBusinessSelfServiceBrandingCommand command,
        CancellationToken cancellationToken = default)
    {
        var business = await _businesses.FindByIdAsync(command.BusinessId, cancellationToken);
        if (business is null)
        {
            return new BusinessSelfServiceBrandingResult(null, "El negocio no existe.");
        }

        var publicName = NormalizeBrandingValue(command.PublicName, business.Name);
        var logoPath = NormalizeBrandingValue(command.LogoPath, business.LogoPath);
        var primaryColor = NormalizeBrandingColor(command.PrimaryColor, DefaultPrimaryColor);
        var secondaryColor = NormalizeBrandingColor(command.SecondaryColor, DefaultSecondaryColor);
        var customFieldColor = NormalizeBrandingColor(command.CustomFieldColor, DefaultCustomFieldColor);
        var stampGoal = NormalizeStampGoal(command.StampGoal);
        var programName = NormalizeBrandingValue(command.ProgramName, DefaultProgramName);
        var programDescription = NormalizeBrandingValue(command.ProgramDescription, DefaultProgramDescription);
        var validationError = ValidateBusinessBranding(
            publicName,
            logoPath,
            primaryColor,
            secondaryColor,
            customFieldColor,
            stampGoal,
            programName,
            programDescription);
        if (validationError is not null)
        {
            return new BusinessSelfServiceBrandingResult(null, validationError);
        }

        await _businessBranding.UpsertAsync(
            new BusinessBranding(
                business.Id,
                publicName,
                logoPath,
                primaryColor,
                secondaryColor,
                customFieldColor,
                stampGoal,
                programName,
                programDescription,
                _clock.UtcNow,
                updatedByAdminUserId: null),
            cancellationToken);

        var settings = await ToBusinessBrandingSettingsAsync(business, cancellationToken);
        return new BusinessSelfServiceBrandingResult(settings, ErrorMessage: null);
    }

    public Task<WalletBrandingRefreshResult> RefreshBusinessWalletBrandingAsync(
        WalletBrandingRefreshCommand command,
        CancellationToken cancellationToken = default)
    {
        return _walletBrandingRefresh.RefreshAsync(
            command.BusinessId,
            limit: 0,
            actorBusinessId: command.BusinessId,
            cancellationToken);
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
        var displayBusiness = await ApplyBrandingAsync(business, cancellationToken);
        var enrollmentUrl = $"{command.BaseUrl.TrimEnd('/')}/Wallet/Select/{publicToken}";
        await _emailSender.SendWalletEnrollmentAsync(
            CreateWalletEnrollmentEmail(client, displayBusiness, enrollmentUrl, command.BaseUrl),
            cancellationToken);

        return new EnrollClientResult(ToDto(card, client, displayBusiness), enrollmentUrl);
    }

    public async Task<WalletLandingDto?> GetWalletLandingAsync(string token, CancellationToken cancellationToken = default)
    {
        var context = await FindCardContextByTokenAsync(token, cancellationToken);
        if (context is null)
        {
            return null;
        }

        var (card, client, business) = context.Value;
        var displayBusiness = await ApplyBrandingAsync(business, cancellationToken);
        return new WalletLandingDto(
            token,
            displayBusiness.DisplayName,
            ShortClientName(client),
            client.UserName,
            card.CurrentStamps,
            displayBusiness.StampGoal,
            card.LifetimeStamps,
            card.GoogleObjectId is not null,
            displayBusiness.LogoPath,
            displayBusiness.PrimaryColor,
            displayBusiness.SecondaryColor,
            displayBusiness.CustomFieldColor,
            displayBusiness.ProgramName,
            RewardText(displayBusiness));
    }

    public async Task<GoogleWalletIssueResult?> SelectGoogleWalletAsync(string token, CancellationToken cancellationToken = default)
    {
        var context = await FindCardContextByTokenAsync(token, cancellationToken);
        if (context is null)
        {
            return null;
        }

        var (card, client, business) = context.Value;
        var displayBusiness = await ApplyBrandingAsync(business, cancellationToken);
        if (card.GoogleObjectId is not null && card.GoogleSaveUrl is not null)
        {
            try
            {
                await _googleWallet.PatchStampStateAsync(card, client, displayBusiness, cancellationToken);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                // Keep the existing save URL usable; explicit refresh and stamp flows surface wallet update warnings.
            }

            return new GoogleWalletIssueResult(card.GoogleObjectId, card.GoogleSaveUrl);
        }

        var result = await _googleWallet.IssueSaveLinkAsync(card, client, displayBusiness, cancellationToken);
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

        var stamped = await AddStampAndNotifyWalletsAsync(
            card,
            client,
            business,
            StampLedgerSource.ModernBusiness,
            actorBusinessId: business.Id,
            cancellationToken);

        if (!stamped)
        {
            throw new InvalidOperationException("La tarjeta ya esta completa. Confirma el canje de recompensa.");
        }

        return ToDto(card, client, await ApplyBrandingAsync(business, cancellationToken));
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

    public async Task<IReadOnlyList<BusinessCardDto>> ListBusinessCardsAsync(
        Guid businessId,
        CancellationToken cancellationToken = default)
    {
        var business = await RequireBusinessAsync(businessId, cancellationToken);
        var cards = await _loyaltyCards.ListByBusinessAsync(business.Id, cancellationToken);
        var results = new List<BusinessCardDto>(cards.Count);

        foreach (var card in cards.OrderByDescending(card => card.LastStampedAt).ThenBy(card => card.CreatedAt))
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

    public async Task<BusinessDashboardDto?> GetBusinessDashboardAsync(
        Guid businessId,
        CancellationToken cancellationToken = default)
    {
        var business = await _businesses.FindByIdAsync(businessId, cancellationToken);
        if (business is null)
        {
            return null;
        }

        var displayBusiness = await ApplyBrandingAsync(business, cancellationToken);

        var cards = await _loyaltyCards.SearchByBusinessAsync(
            business.Id,
            query: string.Empty,
            limit: 25,
            cancellationToken);
        var recentCards = new List<BusinessCardDto>(cards.Count);

        foreach (var card in cards)
        {
            var client = await _clients.FindByIdAsync(card.ClientId, cancellationToken);
            if (client is null)
            {
                continue;
            }

            recentCards.Add(await ToBusinessCardDtoAsync(card, client, business, cancellationToken));
        }

        var events = recentCards
            .SelectMany(card => card.RecentStampEvents.Select(item => new BusinessDashboardStampEventDto(
                card.Id,
                card.Client.UserName,
                $"{card.Client.FirstName} {card.Client.LastName}".Trim(),
                item.CreatedAt,
                item.Source,
                item.PreviousCheckQTY,
                item.NewCheckQTY,
                item.GoogleWalletAttempted,
                item.GoogleWalletSucceeded,
                item.AppleWalletAttempted,
                item.AppleWalletSucceeded,
                item.ErrorSummary)))
            .OrderByDescending(item => item.CreatedAt)
            .Take(8)
            .ToArray();

        return new BusinessDashboardDto(
            ToDto(displayBusiness),
            recentCards.Count,
            recentCards.Sum(card => card.CurrentStamps),
            recentCards.Sum(card => card.LifetimeStamps),
            recentCards.Count(card => card.GoogleIssued),
            recentCards.Count(card => card.AppleTracked),
            recentCards.Sum(card => card.AppleRegisteredDeviceCount),
            events.Count(HasWalletIssue),
            recentCards.Take(8).ToArray(),
            events);
    }

    public async Task<BusinessReportsDto?> GetBusinessReportsAsync(
        Guid businessId,
        CancellationToken cancellationToken = default)
    {
        var business = await _businesses.FindByIdAsync(businessId, cancellationToken);
        if (business is null)
        {
            return null;
        }
        var displayBusiness = await ApplyBrandingAsync(business, cancellationToken);

        var cards = await _loyaltyCards.ListByBusinessAsync(business.Id, cancellationToken);
        var reportCards = new List<BusinessCardDto>(cards.Count);

        foreach (var card in cards)
        {
            var client = await _clients.FindByIdAsync(card.ClientId, cancellationToken);
            if (client is null)
            {
                continue;
            }

            reportCards.Add(await ToBusinessCardDtoAsync(card, client, business, cancellationToken));
        }

        var now = _clock.UtcNow;
        var since30Days = now.AddDays(-30);
        var ledgerRecords = await _stampLedger.ListByBusinessAsync(business.Id, 1000, cancellationToken);
        var redemptionRecords = await _rewardRedemptions.ListByBusinessAsync(business.Id, 1000, cancellationToken);
        var events = ledgerRecords
            .Select(item => new BusinessDashboardStampEventDto(
                item.CardId,
                reportCards.FirstOrDefault(card => card.Id == item.CardId)?.Client.UserName ?? string.Empty,
                reportCards.FirstOrDefault(card => card.Id == item.CardId) is { } card
                    ? $"{card.Client.FirstName} {card.Client.LastName}".Trim()
                    : string.Empty,
                item.CreatedAt,
                item.Source,
                item.PreviousCheckQTY,
                item.NewCheckQTY,
                item.GoogleWalletAttempted,
                item.GoogleWalletSucceeded,
                item.AppleWalletAttempted,
                item.AppleWalletSucceeded,
                item.ErrorSummary))
            .ToArray();
        var reportCulture = CultureInfo.GetCultureInfo("es-MX");
        var recentPeriods = Enumerable.Range(0, 6)
            .Select(index => now.AddMonths(-(5 - index)).ToLocalTime())
            .ToArray();
        var includeReportYear = recentPeriods.Select(period => period.Year).Distinct().Count() > 1;
        var periods = recentPeriods
            .Select(period =>
            {
                var label = FormatReportPeriod(period, includeReportYear, reportCulture);
                var stampCount = ledgerRecords.Count(record =>
                    record.CreatedAt >= now.AddMonths(-6) &&
                    record.CreatedAt.ToLocalTime().Year == period.Year &&
                    record.CreatedAt.ToLocalTime().Month == period.Month &&
                    record.NewCheckQTY > record.PreviousCheckQTY);
                var redemptionCount = redemptionRecords.Count(record =>
                    record.RedeemedAt >= now.AddMonths(-6) &&
                    record.RedeemedAt.ToLocalTime().Year == period.Year &&
                    record.RedeemedAt.ToLocalTime().Month == period.Month);
                return new BusinessReportPeriodDto(label, stampCount, redemptionCount);
            })
            .ToArray();
        var newClientPeriods = recentPeriods
            .Select(period =>
            {
                var label = FormatReportPeriod(period, includeReportYear, reportCulture);
                var newClientCount = reportCards
                    .GroupBy(card => card.Client.Id)
                    .Select(group => group.Min(card => card.CreatedAt))
                    .Count(createdAt => createdAt >= now.AddMonths(-6) &&
                        createdAt.ToLocalTime().Year == period.Year &&
                        createdAt.ToLocalTime().Month == period.Month);
                return new BusinessReportPeriodDto(label, 0, 0, newClientCount);
            })
            .ToArray();
        var businessClients = reportCards
            .GroupBy(card => card.Client.Id)
            .Select(group =>
            {
                var first = group
                    .OrderByDescending(card => card.LastStampedAt)
                    .First();
                return new BusinessReportClientDto(
                    first.Client.Id,
                    first.Client.UserName,
                    $"{first.Client.FirstName} {first.Client.LastName}".Trim(),
                    first.Client.Email,
                    group.Count(),
                    group.Sum(card => card.CurrentStamps),
                    group.Sum(card => card.LifetimeStamps),
                    group.Any(card => !card.IsActive)
                        ? "Inactiva"
                        : group.Any(card => card.GoogleIssued || card.AppleTracked)
                            ? "Lista"
                            : "Pendiente",
                    group.Max(card => card.LastStampedAt));
            })
            .OrderByDescending(client => client.LastActivityAt)
            .ThenBy(client => client.UserName, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var topClients = reportCards
            .GroupBy(card => card.Client.Id)
            .Select(group =>
            {
                var first = group
                    .OrderByDescending(card => card.LastStampedAt)
                    .First();
                return new BusinessReportTopClientDto(
                    first.Client.Id,
                    first.Client.UserName,
                    $"{first.Client.FirstName} {first.Client.LastName}".Trim(),
                    group.Sum(card => card.CurrentStamps),
                    group.Sum(card => card.LifetimeStamps),
                    redemptionRecords.Count(record => record.UserId == first.Client.Id),
                    group.Max(card => card.LastStampedAt));
            })
            .OrderByDescending(client => client.LifetimeStamps)
            .ThenByDescending(client => client.CurrentStamps)
            .ThenByDescending(client => client.LastActivityAt)
            .Take(5)
            .ToArray();
        var walletIssues = events
            .Where(HasWalletIssue)
            .OrderByDescending(item => item.CreatedAt)
            .Take(10)
            .ToArray();
        var stampsLast30Days = ledgerRecords.Count(item => item.CreatedAt >= since30Days && item.NewCheckQTY > item.PreviousCheckQTY);
        var redemptionsLast30Days = redemptionRecords.Count(item => item.RedeemedAt >= since30Days);
        var redemptionRate = stampsLast30Days == 0
            ? 0m
            : Math.Round((decimal)redemptionsLast30Days / stampsLast30Days * 100m, 1, MidpointRounding.AwayFromZero);

        return new BusinessReportsDto(
            ToDto(displayBusiness),
            reportCards.Count,
            reportCards.Count(card => card.CreatedAt >= since30Days),
            reportCards.Select(card => card.Client.Id).Distinct().Count(),
            reportCards.Sum(card => card.CurrentStamps),
            reportCards.Sum(card => card.LifetimeStamps),
            stampsLast30Days,
            reportCards.Count(card => card.GoogleIssued || card.AppleTracked),
            reportCards.Count(card => !card.GoogleIssued && !card.AppleTracked),
            reportCards.Count(card => card.GoogleIssued),
            reportCards.Count(card => !card.GoogleIssued),
            reportCards.Count(card => card.AppleTracked),
            reportCards.Count(card => !card.AppleTracked),
            reportCards.Sum(card => card.AppleRegisteredDeviceCount),
            walletIssues.Length,
            redemptionsLast30Days,
            redemptionRate,
            periods,
            newClientPeriods,
            businessClients,
            topClients,
            walletIssues);
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

    public async Task<BusinessCardDto?> GetBusinessCardForClientAsync(
        Guid businessId,
        string userNameOrEmail,
        CancellationToken cancellationToken = default)
    {
        var business = await RequireBusinessAsync(businessId, cancellationToken);
        var client = await RequireClientAsync(userNameOrEmail, cancellationToken);
        var card = await _loyaltyCards.FindByClientAndBusinessAsync(client.Id, business.Id, cancellationToken);
        return card is null
            ? null
            : await ToBusinessCardDtoAsync(card, client, business, cancellationToken);
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
        if (!await IsCardActiveAsync(card.Id, cancellationToken))
        {
            return null;
        }

        var stamped = await AddStampAndNotifyWalletsAsync(
            card,
            client,
            business,
            StampLedgerSource.ModernBusiness,
            actorBusinessId: business.Id,
            cancellationToken);

        if (!stamped)
        {
            return await ToBusinessCardDtoAsync(card, client, business, cancellationToken);
        }

        return await ToBusinessCardDtoAsync(card, client, business, cancellationToken);
    }

    public async Task<RewardRedemptionResult> RedeemRewardAsync(
        Guid businessId,
        Guid cardId,
        CancellationToken cancellationToken = default)
    {
        var context = await FindBusinessCardContextAsync(businessId, cardId, cancellationToken);
        if (context is null)
        {
            return new RewardRedemptionResult(false, null, null, "La tarjeta no existe para este negocio.", false);
        }

        var (card, client, business) = context.Value;
        if (!await IsCardActiveAsync(card.Id, cancellationToken))
        {
            return new RewardRedemptionResult(false, null, null, "La tarjeta esta desactivada para este negocio.", false);
        }

        var displayBusiness = await ApplyBrandingAsync(business, cancellationToken);
        if (!IsRewardReady(card, displayBusiness))
        {
            return new RewardRedemptionResult(false, await ToBusinessCardDtoAsync(card, client, business, cancellationToken), null, "La tarjeta aun no esta completa.", false);
        }

        if (!await _rewardRedemptions.IsAvailableAsync(cancellationToken))
        {
            _logger.LogError(
                "Reward redemption storage is unavailable. Apply migration 118 before redeeming rewards.");
            return new RewardRedemptionResult(
                false,
                await ToBusinessCardDtoAsync(card, client, business, cancellationToken),
                null,
                RewardRedemptionTableMissingMessage,
                false);
        }

        var previousCheckQty = card.CurrentStamps;
        var previousHistoricCheckQty = card.LifetimeStamps;
        var redeemedAt = _clock.UtcNow;
        card.RedeemReward(redeemedAt, displayBusiness.StampGoal);
        await _loyaltyCards.UpdateAsync(card, cancellationToken);
        card = await _loyaltyCards.FindByIdAsync(card.Id, cancellationToken) ?? card;

        var googleAttempted = false;
        var googleSucceeded = false;
        var appleAttempted = false;
        var appleSucceeded = false;
        string? errorSummary = null;

        try
        {
            if (card.GoogleObjectId is not null)
            {
                googleAttempted = true;
                await _googleWallet.PatchStampStateAsync(card, client, displayBusiness, cancellationToken);
                googleSucceeded = true;
            }

            appleAttempted = true;
            await _appleWallet.NotifyPassUpdatedAsync(card, client, displayBusiness, cancellationToken);
            appleSucceeded = true;
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            errorSummary = SafeErrorSummary(exception);
        }

        var redemption = new RewardRedemptionRecord(
            0,
            card.Id,
            card.BusinessId,
            card.ClientId,
            business.Id,
            displayBusiness.StampGoal,
            previousCheckQty,
            previousHistoricCheckQty,
            RewardText(displayBusiness),
            googleAttempted,
            googleSucceeded,
            appleAttempted,
            appleSucceeded,
            errorSummary,
            redeemedAt,
            _clock.UtcNow);

        await _rewardRedemptions.AddAsync(redemption, cancellationToken);
        await RecordStampLedgerAsync(
            card,
            StampLedgerSource.RewardRedeemed,
            business.Id,
            previousCheckQty,
            previousHistoricCheckQty,
            googleAttempted,
            googleSucceeded,
            appleAttempted,
            appleSucceeded,
            errorSummary,
            cancellationToken);

        var dto = ToRewardRedemptionDto(redemption);
        return new RewardRedemptionResult(
            true,
            await ToBusinessCardDtoAsync(card, client, business, cancellationToken),
            dto,
            errorSummary is null ? null : "Recompensa canjeada, pero la tarjeta digital quedo con alerta de actualizacion.",
            errorSummary is not null);
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
        if (!await IsCardActiveAsync(card.Id, cancellationToken))
        {
            return null;
        }

        var publicToken = await _walletLinkTokens.CreateTokenAsync(
            card.Id,
            WalletLinkPurposes.WalletSelect,
            cancellationToken);
        var displayBusiness = await ApplyBrandingAsync(business, cancellationToken);
        var enrollmentUrl = $"{baseUrl.TrimEnd('/')}/Wallet/Select/{publicToken}";
        await _emailSender.SendWalletEnrollmentAsync(
            CreateWalletEnrollmentEmail(client, displayBusiness, enrollmentUrl, baseUrl),
            cancellationToken);

        return new ResendWalletEmailResult(
            await ToBusinessCardDtoAsync(card, client, displayBusiness, cancellationToken),
            enrollmentUrl);
    }

    public async Task<BusinessCardLifecycleResult> SetBusinessCardActiveAsync(
        Guid businessId,
        Guid cardId,
        bool isActive,
        CancellationToken cancellationToken = default)
    {
        var context = await FindBusinessCardContextAsync(businessId, cardId, cancellationToken);
        if (context is null)
        {
            return new BusinessCardLifecycleResult(false, null, "La tarjeta no existe para este negocio.");
        }

        var (card, client, business) = context.Value;
        await _accountLifecycle.SetCardActiveAsync(
            new ClientCardLifecycleRecord(
                card.Id,
                business.Id,
                isActive,
                _clock.UtcNow,
                business.Id),
            cancellationToken);

        return new BusinessCardLifecycleResult(
            true,
            await ToBusinessCardDtoAsync(card, client, business, cancellationToken),
            ErrorMessage: null);
    }

    public async Task<BusinessCardLifecycleResult> DeleteBusinessCardAsync(
        Guid businessId,
        Guid cardId,
        CancellationToken cancellationToken = default)
    {
        var context = await FindBusinessCardContextAsync(businessId, cardId, cancellationToken);
        if (context is null)
        {
            return new BusinessCardLifecycleResult(false, null, "La tarjeta no existe para este negocio.");
        }

        var deleted = await _accountLifecycle.DeleteBusinessCardAsync(
            businessId,
            cardId,
            cancellationToken);

        return deleted
            ? new BusinessCardLifecycleResult(true, null, ErrorMessage: null)
            : new BusinessCardLifecycleResult(false, null, "No se pudo eliminar la tarjeta.");
    }

    public async Task<IReadOnlyList<LoyaltyCardDto>> GetClientCardsAsync(string userNameOrEmail, CancellationToken cancellationToken = default)
    {
        var client = await RequireClientAsync(userNameOrEmail, cancellationToken);
        return await GetClientCardsByClientAsync(client, cancellationToken);
    }

    public async Task<IReadOnlyList<LoyaltyCardDto>> GetClientCardsByClientIdAsync(
        Guid clientId,
        CancellationToken cancellationToken = default)
    {
        var client = await _clients.FindByIdAsync(clientId, cancellationToken)
            ?? throw new InvalidOperationException("Client was not found.");
        return await GetClientCardsByClientAsync(client, cancellationToken);
    }

    public async Task<IReadOnlyList<ClientLoyaltyCardDto>> GetClientDashboardCardsAsync(
        Guid clientId,
        CancellationToken cancellationToken = default)
    {
        var dashboard = await GetClientDashboardAsync(clientId, cancellationToken);
        return dashboard.Cards;
    }

    public async Task<ClientDashboardDto> GetClientDashboardAsync(
        Guid clientId,
        CancellationToken cancellationToken = default)
    {
        var client = await _clients.FindByIdAsync(clientId, cancellationToken)
            ?? throw new InvalidOperationException("Client was not found.");
        var cards = await _loyaltyCards.ListByClientAsync(client.Id, cancellationToken);
        var businesses = await _businesses.ListAsync(cancellationToken);
        var results = new List<ClientLoyaltyCardDto>(cards.Count);

        foreach (var card in cards)
        {
            var token = await _walletLinkTokens.CreateTokenAsync(
                card.Id,
                WalletLinkPurposes.WalletSelect,
                cancellationToken);
            var business = businesses.Single(existing => existing.Id == card.BusinessId);
            business = await ApplyBrandingAsync(business, cancellationToken);
            var recentRedemptions = await _rewardRedemptions.ListRecentByCardIdAsync(card.Id, 3, cancellationToken);
            results.Add(new ClientLoyaltyCardDto(
                card.Id,
                token,
                business.DisplayName,
                client.UserName,
                card.CurrentStamps,
                business.StampGoal,
                card.LifetimeStamps,
                card.LastStampedAt,
                card.GoogleObjectId is not null,
                card.GoogleSaveUrl,
                AppleTracked: false,
                AppleRegisteredDeviceCount: 0,
                AppleUpdatedAt: null,
                recentRedemptions.Select(ToRewardRedemptionDto).ToArray(),
                business.PrimaryColor,
                business.SecondaryColor));
        }

        for (var index = 0; index < results.Count; index++)
        {
            var card = cards[index];
            var applePass = await _appleWalletPasses.FindPassByCardIdAsync(card.Id, cancellationToken);
            if (applePass is null)
            {
                continue;
            }

            var devices = await _appleWalletPasses.ListDevicesForPassAsync(
                applePass.PassTypeIdentifier,
                applePass.SerialNumber,
                cancellationToken);

            results[index] = results[index] with
            {
                AppleTracked = true,
                AppleRegisteredDeviceCount = devices.Count,
                AppleUpdatedAt = applePass.UpdatedAt
            };
        }

        return new ClientDashboardDto(
            ToDto(client),
            results,
            results.Sum(card => card.CurrentStamps),
            results.Sum(card => card.LifetimeStamps),
            results.Count(card => card.GoogleIssued),
            results.Count(card => card.AppleTracked));
    }

    private async Task<IReadOnlyList<LoyaltyCardDto>> GetClientCardsByClientAsync(
        Client client,
        CancellationToken cancellationToken)
    {
        var cards = await _loyaltyCards.ListByClientAsync(client.Id, cancellationToken);
        var businesses = await _businesses.ListAsync(cancellationToken);

        var results = new List<LoyaltyCardDto>(cards.Count);
        foreach (var card in cards)
        {
            var business = businesses.Single(existing => existing.Id == card.BusinessId);
            results.Add(ToDto(card, client, await ApplyBrandingAsync(business, cancellationToken)));
        }

        return results.ToArray();
    }

    private async Task<(string Token, DateTimeOffset ExpiresAt)> CreatePasswordResetTokenAsync(
        PasswordResetAccountType accountType,
        Guid accountId,
        CancellationToken cancellationToken)
    {
        const int maxAttempts = 3;

        var now = _clock.UtcNow;
        var expiresAt = now.AddHours(1);
        await _passwordResetTokens.RevokeActiveByAccountAsync(
            accountType,
            accountId,
            now,
            cancellationToken);

        for (var attempt = 0; attempt < maxAttempts; attempt++)
        {
            var plainToken = CreateOpaqueToken();
            var hash = HashToken(plainToken);
            var existing = await _passwordResetTokens.FindActiveByTokenHashAsync(
                hash,
                accountType,
                now,
                cancellationToken);
            if (existing is not null)
            {
                continue;
            }

            await _passwordResetTokens.AddAsync(
                new PasswordResetTokenRecord(
                    0,
                    accountType,
                    accountId,
                    hash,
                    Suffix(plainToken),
                    now,
                    expiresAt,
                    UsedAt: null,
                    RevokedAt: null),
                cancellationToken);

            return (plainToken, expiresAt);
        }

        throw new InvalidOperationException("Could not create a unique password reset token.");
    }

    private async Task<PasswordResetTokenRecord?> FindPasswordResetTokenAsync(
        string token,
        PasswordResetAccountType accountType,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return null;
        }

        return await _passwordResetTokens.FindActiveByTokenHashAsync(
            HashToken(token.Trim()),
            accountType,
            _clock.UtcNow,
            cancellationToken);
    }

    private static string? ValidateResetPasswordCommand(ResetPasswordCommand command)
    {
        if (string.IsNullOrWhiteSpace(command.Token))
        {
            return "El link no es valido o ya expiro.";
        }

        if (string.IsNullOrWhiteSpace(command.NewPassword))
        {
            return "La contrasena nueva es requerida.";
        }

        if (command.NewPassword.Length < 8)
        {
            return "La contrasena nueva debe tener al menos 8 caracteres.";
        }

        if (command.NewPassword.Length > 128)
        {
            return "La contrasena nueva no puede exceder 128 caracteres.";
        }

        return null;
    }

    private async Task<Business> RequireBusinessAsync(Guid businessId, CancellationToken cancellationToken)
    {
        return await _businesses.FindByIdAsync(businessId, cancellationToken)
            ?? throw new InvalidOperationException("Business was not found.");
    }

    private async Task<Client> RequireClientAsync(string userNameOrEmail, CancellationToken cancellationToken)
    {
        return await _clients.FindByUserNameOrEmailAsync(
                ClientUserNameNormalizer.NormalizeUserNameOrEmail(userNameOrEmail),
                cancellationToken)
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

        return client is null || business is null
            ? null
            : (card, client, await ApplyBrandingAsync(business, cancellationToken));
    }

    private async Task<bool> VerifyClientPasswordAsync(
        Client client,
        string password,
        bool migrateLegacy,
        CancellationToken cancellationToken)
    {
        var subject = new ClientPasswordHashSubject(client.Id);
        var credential = await _clientCredentials.FindByClientIdAsync(client.Id, cancellationToken);
        if (credential is not null)
        {
            var verification = _clientPasswordHasher.VerifyHashedPassword(
                subject,
                credential.PasswordHash,
                password);
            if (verification == PasswordVerificationResult.Failed)
            {
                return false;
            }

            if (verification == PasswordVerificationResult.SuccessRehashNeeded)
            {
                await _clientCredentials.UpsertAsync(
                    credential.Rehash(_clientPasswordHasher.HashPassword(subject, password), _clock.UtcNow),
                    cancellationToken);
            }

            return true;
        }

        if (string.IsNullOrWhiteSpace(client.PasswordHashPlaceholder) ||
            !LegacyPasswordVerifier.Matches(client.PasswordHashPlaceholder, password))
        {
            return false;
        }

        if (migrateLegacy)
        {
            var now = _clock.UtcNow;
            await _clientCredentials.UpsertAsync(
                new ClientCredential(
                    client.Id,
                    _clientPasswordHasher.HashPassword(subject, password),
                    now,
                    now),
                cancellationToken);
        }

        return true;
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

    private async Task<Business> ApplyBrandingAsync(Business business, CancellationToken cancellationToken)
    {
        var branding = await _businessBranding.FindByBusinessIdAsync(business.Id, cancellationToken);
        if (branding is null)
        {
            return business;
        }

        return new Business(
            business.Id,
            business.Name,
            business.Email,
            business.PasswordHashPlaceholder,
            string.IsNullOrWhiteSpace(branding.LogoPath) ? business.LogoPath : branding.LogoPath,
            branding.PublicName,
            branding.PrimaryColor,
            branding.SecondaryColor,
            branding.ProgramName,
            branding.ProgramDescription,
            branding.CustomFieldColor,
            branding.StampGoal);
    }

    private async Task<BusinessBrandingSettingsDto> ToBusinessBrandingSettingsAsync(
        Business business,
        CancellationToken cancellationToken)
    {
        var branding = await _businessBranding.FindByBusinessIdAsync(business.Id, cancellationToken);
        return new BusinessBrandingSettingsDto(
            business.Id,
            branding?.PublicName ?? business.DisplayName,
            business.Email,
            ToBrandingDto(business, branding));
    }

    private static BusinessBrandingDto ToBrandingDto(Business business, BusinessBranding? branding)
    {
        return new BusinessBrandingDto(
            branding?.PublicName ?? business.Name,
            branding?.LogoPath ?? business.LogoPath,
            branding?.PrimaryColor ?? DefaultPrimaryColor,
            branding?.SecondaryColor ?? DefaultSecondaryColor,
            branding?.CustomFieldColor ?? DefaultCustomFieldColor,
            branding?.StampGoal ?? Business.DefaultStampGoal,
            branding?.ProgramName ?? DefaultProgramName,
            branding?.ProgramDescription ?? DefaultProgramDescription,
            branding?.UpdatedAt);
    }

    private WalletEnrollmentEmail CreateWalletEnrollmentEmail(
        Client client,
        Business business,
        string enrollmentUrl,
        string baseUrl)
    {
        return new WalletEnrollmentEmail(
            client.Email,
            client.FullName,
            business.DisplayName,
            enrollmentUrl,
            _clock.UtcNow,
            BuildPublicAssetUrl(business.LogoPath, baseUrl),
            business.PrimaryColor,
            business.ProgramName);
    }

    private static string? BuildPublicAssetUrl(string logoPath, string baseUrl)
    {
        if (string.IsNullOrWhiteSpace(logoPath))
        {
            return null;
        }

        if (Uri.TryCreate(logoPath, UriKind.Absolute, out var absolute) &&
            (absolute.Scheme == Uri.UriSchemeHttp || absolute.Scheme == Uri.UriSchemeHttps))
        {
            return absolute.ToString();
        }

        if (!logoPath.StartsWith("/", StringComparison.Ordinal))
        {
            return null;
        }

        return $"{baseUrl.TrimEnd('/')}{logoPath}";
    }

    private static ClientDto ToDto(Client client)
    {
        return new ClientDto(client.Id, client.UserName, client.FirstName, client.LastName, client.Email);
    }

    private static BusinessDto ToDto(Business business)
    {
        return new BusinessDto(business.Id, business.DisplayName, business.Email, business.LogoPath);
    }

    private static LoyaltyCardDto ToDto(LoyaltyCard card, Client client, Business business)
    {
        var progress = CalculateStampProgress(card.CurrentStamps, business.StampGoal);
        return new LoyaltyCardDto(
            card.Id,
            card.EnrollmentToken,
            business.DisplayName,
            client.UserName,
            card.CurrentStamps,
            progress.StampGoal,
            card.LifetimeStamps,
            progress.VisibleStamps,
            progress.RewardReady,
            progress.StampsRemaining,
            card.GoogleObjectId,
            card.GoogleSaveUrl);
    }

    private async Task<BusinessCardDto> ToBusinessCardDtoAsync(
        LoyaltyCard card,
        Client client,
        Business business,
        CancellationToken cancellationToken)
    {
        var displayBusiness = await ApplyBrandingAsync(business, cancellationToken);
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

        var recentEvents = await _stampLedger.ListRecentByCardIdAsync(card.Id, 5, cancellationToken);
        var recentRedemptions = await _rewardRedemptions.ListRecentByCardIdAsync(card.Id, 5, cancellationToken);
        var isActive = await IsCardActiveAsync(card.Id, cancellationToken);
        var progress = CalculateStampProgress(card.CurrentStamps, displayBusiness.StampGoal);

        return new BusinessCardDto(
            card.Id,
            card.EnrollmentToken,
            ToDto(client),
            displayBusiness.DisplayName,
            card.CreatedAt,
            card.CurrentStamps,
            progress.StampGoal,
            card.LifetimeStamps,
            progress.VisibleStamps,
            progress.RewardReady,
            progress.StampsRemaining,
            card.LastStampedAt,
            !string.IsNullOrWhiteSpace(card.GoogleObjectId),
            applePass is not null,
            appleDeviceCount,
            applePass?.UpdatedAt,
            isActive,
            recentEvents.Select(ToStampLedgerEventDto).ToArray(),
            recentRedemptions.Select(ToRewardRedemptionDto).ToArray());
    }

    private async Task<bool> IsCardActiveAsync(Guid cardId, CancellationToken cancellationToken)
    {
        var status = await _accountLifecycle.FindCardLifecycleAsync(cardId, cancellationToken);
        return status?.IsActive ?? true;
    }

    private async Task<bool> AddStampAndNotifyWalletsAsync(
        LoyaltyCard card,
        Client client,
        Business business,
        StampLedgerSource source,
        Guid? actorBusinessId,
        CancellationToken cancellationToken)
    {
        var previousCheckQty = card.CurrentStamps;
        var previousHistoricCheckQty = card.LifetimeStamps;
        var displayBusiness = await ApplyBrandingAsync(business, cancellationToken);
        if (IsRewardReady(card, displayBusiness))
        {
            return false;
        }

        card.AddStamp(_clock.UtcNow, displayBusiness.StampGoal);
        await _loyaltyCards.UpdateAsync(card, cancellationToken);

        var googleAttempted = false;
        var googleSucceeded = false;
        var appleAttempted = false;
        var appleSucceeded = false;

        try
        {
            if (card.GoogleObjectId is not null)
            {
                googleAttempted = true;
                await _googleWallet.PatchStampStateAsync(card, client, displayBusiness, cancellationToken);
                googleSucceeded = true;
            }

            appleAttempted = true;
            await _appleWallet.NotifyPassUpdatedAsync(card, client, displayBusiness, cancellationToken);
            appleSucceeded = true;

            await RecordStampLedgerAsync(
                card,
                source,
                actorBusinessId,
                previousCheckQty,
                previousHistoricCheckQty,
                googleAttempted,
                googleSucceeded,
                appleAttempted,
                appleSucceeded,
                errorSummary: null,
                cancellationToken);
            return true;
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            await RecordStampLedgerAsync(
                card,
                source,
                actorBusinessId,
                previousCheckQty,
                previousHistoricCheckQty,
                googleAttempted,
                googleSucceeded,
                appleAttempted,
                appleSucceeded,
                SafeErrorSummary(exception),
                cancellationToken);
            throw;
        }
    }

    private async Task RecordStampLedgerAsync(
        LoyaltyCard card,
        StampLedgerSource source,
        Guid? actorBusinessId,
        int previousCheckQty,
        int previousHistoricCheckQty,
        bool googleAttempted,
        bool googleSucceeded,
        bool appleAttempted,
        bool appleSucceeded,
        string? errorSummary,
        CancellationToken cancellationToken)
    {
        await _stampLedger.AddAsync(
            new StampLedgerRecord(
                0,
                card.Id,
                card.BusinessId,
                card.ClientId,
                source,
                actorBusinessId,
                previousCheckQty,
                card.CurrentStamps,
                previousHistoricCheckQty,
                card.LifetimeStamps,
                card.LastStampedAt,
                googleAttempted,
                googleSucceeded,
                appleAttempted,
                appleSucceeded,
                errorSummary,
                _clock.UtcNow),
            cancellationToken);
    }

    private static StampLedgerEventDto ToStampLedgerEventDto(StampLedgerRecord record)
    {
        return new StampLedgerEventDto(
            record.CreatedAt,
            record.Source,
            record.PreviousCheckQTY,
            record.NewCheckQTY,
            record.PreviousHistoricCheckQTY,
            record.NewHistoricCheckQTY,
            record.ObservedLastCheck,
            record.GoogleWalletAttempted,
            record.GoogleWalletSucceeded,
            record.AppleWalletAttempted,
            record.AppleWalletSucceeded,
            record.ErrorSummary);
    }

    private static RewardRedemptionDto ToRewardRedemptionDto(RewardRedemptionRecord record)
    {
        return new RewardRedemptionDto(
            record.CardId,
            record.StampGoal,
            record.RedeemedCheckQTY,
            record.HistoricCheckQTY,
            record.RewardText,
            record.GoogleWalletAttempted,
            record.GoogleWalletSucceeded,
            record.AppleWalletAttempted,
            record.AppleWalletSucceeded,
            record.ErrorSummary,
            record.RedeemedAt);
    }

    private static bool IsRewardReady(LoyaltyCard card, Business business)
    {
        return CalculateStampProgress(card.CurrentStamps, business.StampGoal).RewardReady;
    }

    private static StampProgress CalculateStampProgress(int currentStamps, int stampGoal)
    {
        var goal = NormalizeStampGoal(stampGoal);
        var visibleStamps = Math.Min(Math.Max(currentStamps, 0), goal);
        var rewardReady = visibleStamps >= goal;
        return new StampProgress(
            goal,
            visibleStamps,
            rewardReady,
            Math.Max(0, goal - visibleStamps));
    }

    private sealed record StampProgress(int StampGoal, int VisibleStamps, bool RewardReady, int StampsRemaining);

    private static string RewardText(Business business)
    {
        return string.IsNullOrWhiteSpace(business.ProgramDescription)
            ? NormalizeBrandingValue(business.ProgramName ?? DefaultProgramName, DefaultProgramName)
            : business.ProgramDescription;
    }

    private static string ShortClientName(Client client)
    {
        var firstName = FirstToken(client.FirstName);
        var lastName = FirstToken(client.LastName);
        var displayName = $"{firstName} {lastName}".Trim();
        return string.IsNullOrWhiteSpace(displayName) ? client.UserName : displayName;
    }

    private static string FirstToken(string value)
    {
        return value.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? string.Empty;
    }

    private static string SafeErrorSummary(Exception exception)
    {
        return exception.GetType().Name;
    }

    private static bool HasWalletIssue(BusinessDashboardStampEventDto item)
    {
        return !string.IsNullOrWhiteSpace(item.ErrorSummary) ||
            (item.GoogleWalletAttempted && !item.GoogleWalletSucceeded) ||
            (item.AppleWalletAttempted && !item.AppleWalletSucceeded);
    }

    private static string? ValidateClientProfile(string firstName, string lastName, string email)
    {
        if (string.IsNullOrWhiteSpace(firstName))
        {
            return "El nombre es requerido.";
        }

        if (firstName.Length > ClientNameMaxLength)
        {
            return $"El nombre no puede exceder {ClientNameMaxLength} caracteres.";
        }

        if (string.IsNullOrWhiteSpace(lastName))
        {
            return "El apellido es requerido.";
        }

        if (lastName.Length > ClientNameMaxLength)
        {
            return $"El apellido no puede exceder {ClientNameMaxLength} caracteres.";
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            return "El correo es requerido.";
        }

        if (email.Length > ClientEmailMaxLength)
        {
            return $"El correo no puede exceder {ClientEmailMaxLength} caracteres.";
        }

        if (!email.Contains('@', StringComparison.Ordinal) || email[0] == '@' || email[^1] == '@')
        {
            return "El correo no tiene un formato valido.";
        }

        return null;
    }

    private static string? ValidateBusinessBranding(
        string publicName,
        string logoPath,
        string primaryColor,
        string secondaryColor,
        string customFieldColor,
        int stampGoal,
        string programName,
        string programDescription)
    {
        if (string.IsNullOrWhiteSpace(publicName))
        {
            return "El nombre del negocio es requerido.";
        }

        if (publicName.Length > BrandingNameMaxLength)
        {
            return $"El nombre del negocio no puede exceder {BrandingNameMaxLength} caracteres.";
        }

        if (logoPath.Length > BrandingLogoMaxLength)
        {
            return $"El logo de marca no puede exceder {BrandingLogoMaxLength} caracteres.";
        }

        if (!IsHexColor(primaryColor))
        {
            return "El color primario debe usar formato #RRGGBB.";
        }

        if (!IsHexColor(secondaryColor))
        {
            return "El color secundario debe usar formato #RRGGBB.";
        }

        if (!IsHexColor(customFieldColor))
        {
            return "El color de campos personalizados debe usar formato #RRGGBB.";
        }

        if (stampGoal <= 0 || stampGoal > MaxStampGoal)
        {
            return $"El numero de sellos debe ser entre 1 y {MaxStampGoal}.";
        }

        if (string.IsNullOrWhiteSpace(programName))
        {
            return "El nombre del programa es requerido.";
        }

        if (programName.Length > BrandingNameMaxLength)
        {
            return $"El nombre del programa no puede exceder {BrandingNameMaxLength} caracteres.";
        }

        if (programDescription.Length > BrandingDescriptionMaxLength)
        {
            return $"La descripcion del programa no puede exceder {BrandingDescriptionMaxLength} caracteres.";
        }

        return null;
    }

    private static string NormalizeBrandingValue(string value, string fallback)
    {
        return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
    }

    private static string NormalizeBrandingColor(string value, string fallback)
    {
        var normalized = NormalizeBrandingValue(value, fallback);
        return normalized.Length == 6 && normalized[0] != '#'
            ? $"#{normalized}"
            : normalized;
    }

    private static int NormalizeStampGoal(int stampGoal)
    {
        return stampGoal <= 0 ? Business.DefaultStampGoal : stampGoal;
    }

    private static string FormatReportPeriod(DateTimeOffset period, bool includeYear, CultureInfo culture)
    {
        var format = includeYear ? "MMM yyyy" : "MMMM";
        return culture.TextInfo.ToTitleCase(period.ToString(format, culture));
    }

    private static string NormalizeConsentValue(string value, string fallback)
    {
        var normalized = string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
        return normalized.Length <= 64 ? normalized : normalized[..64];
    }

    private static bool IsHexColor(string value)
    {
        return value.Length == 7 &&
            value[0] == '#' &&
            value.Skip(1).All(character =>
                (character >= '0' && character <= '9') ||
                (character >= 'a' && character <= 'f') ||
                (character >= 'A' && character <= 'F'));
    }

    private static string CreateOpaqueToken()
    {
        Span<byte> bytes = stackalloc byte[32];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    private static string HashToken(string token)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static string Suffix(string token)
    {
        return token.Length <= 8 ? token : token[^8..];
    }
}
