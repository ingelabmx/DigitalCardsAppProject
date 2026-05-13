using DigitalCards.Application.Abstractions;
using DigitalCards.Application.Models;
using DigitalCards.Domain;
using Microsoft.AspNetCore.Identity;

namespace DigitalCards.Application.Services;

public sealed class AdminAppService
{
    private const int AdminUserNameMaxLength = 15;
    private const int AdminNameMaxLength = 30;
    private const int AdminEmailMaxLength = 30;
    private const int BusinessNameMaxLength = 30;
    private const int BusinessEmailMaxLength = 30;
    private const int BusinessLogoMaxLength = 100;
    private const int BrandingNameMaxLength = 80;
    private const int BrandingLogoMaxLength = 200;
    private const int BrandingDescriptionMaxLength = 280;
    private const int NotesMaxLength = 500;
    private const string DefaultBusinessLogoPath = "/img/demo-coffee.svg";
    private const string DefaultPrimaryColor = "#111827";
    private const string DefaultSecondaryColor = "#2563eb";
    private const string DefaultProgramName = "Tarjeta de lealtad";
    private const string DefaultProgramDescription = "Acumula sellos digitales y consulta tu tarjeta en Wallet.";
    private const string DuplicateAdminMessage = "An admin user with the same username or email already exists.";
    private const string DuplicateBusinessMessage = "A business with the same name or email already exists.";

    private readonly IAdminCredentialRepository _adminCredentials;
    private readonly IAdminUserRepository _adminUsers;
    private readonly IBusinessBrandingRepository _businessBranding;
    private readonly IBusinessCredentialRepository _businessCredentials;
    private readonly IBusinessRepository _businesses;
    private readonly IClientRepository _clients;
    private readonly IClock _clock;
    private readonly IAppleWalletPassRepository _appleWalletPasses;
    private readonly ILoyaltyCardRepository _loyaltyCards;
    private readonly IPasswordHasher<AdminPasswordHashSubject> _adminPasswordHasher;
    private readonly IPasswordHasher<BusinessPasswordHashSubject> _businessPasswordHasher;
    private readonly IPilotBusinessRepository _pilotBusinesses;
    private readonly IPilotClientRepository _pilotClients;
    private readonly IStampLedgerRepository _stampLedger;

    public AdminAppService(
        IAdminUserRepository adminUsers,
        IAdminCredentialRepository adminCredentials,
        IBusinessRepository businesses,
        IBusinessBrandingRepository businessBranding,
        IBusinessCredentialRepository businessCredentials,
        IClientRepository clients,
        ILoyaltyCardRepository loyaltyCards,
        IAppleWalletPassRepository appleWalletPasses,
        IStampLedgerRepository stampLedger,
        IPilotBusinessRepository pilotBusinesses,
        IPilotClientRepository pilotClients,
        IClock clock,
        IPasswordHasher<AdminPasswordHashSubject> adminPasswordHasher,
        IPasswordHasher<BusinessPasswordHashSubject> businessPasswordHasher)
    {
        _adminUsers = adminUsers;
        _adminCredentials = adminCredentials;
        _businesses = businesses;
        _businessBranding = businessBranding;
        _businessCredentials = businessCredentials;
        _clients = clients;
        _loyaltyCards = loyaltyCards;
        _appleWalletPasses = appleWalletPasses;
        _stampLedger = stampLedger;
        _pilotBusinesses = pilotBusinesses;
        _pilotClients = pilotClients;
        _clock = clock;
        _adminPasswordHasher = adminPasswordHasher;
        _businessPasswordHasher = businessPasswordHasher;
    }

    public async Task<AdminUserDto?> LoginAdminAsync(
        AdminLoginCommand command,
        CancellationToken cancellationToken = default)
    {
        var admin = await _adminUsers.FindByUserNameOrEmailAsync(
            command.UserNameOrEmail,
            cancellationToken);

        if (admin is null)
        {
            return null;
        }

        var subject = new AdminPasswordHashSubject(admin.Id);
        var credential = await _adminCredentials.FindByAdminUserIdAsync(admin.Id, cancellationToken);
        if (credential is not null)
        {
            var verification = _adminPasswordHasher.VerifyHashedPassword(
                subject,
                credential.PasswordHash,
                command.Password);

            if (verification == PasswordVerificationResult.Failed)
            {
                return null;
            }

            if (verification == PasswordVerificationResult.SuccessRehashNeeded)
            {
                await _adminCredentials.UpsertAsync(
                    credential.Rehash(_adminPasswordHasher.HashPassword(subject, command.Password), _clock.UtcNow),
                    cancellationToken);
            }

            return ToDto(admin);
        }

        if (!LegacyPasswordVerifier.Matches(admin.PasswordHashPlaceholder, command.Password))
        {
            return null;
        }

        var now = _clock.UtcNow;
        await _adminCredentials.UpsertAsync(
            new AdminCredential(
                admin.Id,
                _adminPasswordHasher.HashPassword(subject, command.Password),
                now,
                now),
            cancellationToken);

        return ToDto(admin);
    }

    public async Task<IReadOnlyList<AdminUserListItemDto>> ListAdminUsersAsync(
        CancellationToken cancellationToken = default)
    {
        var admins = await _adminUsers.ListAsync(cancellationToken);
        return admins.Select(ToListItemDto).ToArray();
    }

    public async Task<AdminAccessResult> CreateAdminAsync(
        CreateAdminCommand command,
        CancellationToken cancellationToken = default)
    {
        var userName = command.UserName.Trim();
        var firstName = command.FirstName.Trim();
        var lastName = command.LastName.Trim();
        var email = command.Email.Trim().ToLowerInvariant();

        var validationError = ValidateCreateAdminCommand(
            userName,
            firstName,
            lastName,
            email,
            command.InitialPassword,
            command.ActingAdminUserId);
        if (validationError is not null)
        {
            return FailedAdminAccess(validationError);
        }

        if (await _adminUsers.FindByUserNameOrEmailAsync(userName, cancellationToken) is not null ||
            await _adminUsers.FindByUserNameOrEmailAsync(email, cancellationToken) is not null)
        {
            return FailedAdminAccess("Ya existe un admin con ese usuario o correo.");
        }

        var legacyPasswordHash = LegacyPasswordVerifier.CreateLegacyBusinessPasswordHash(command.InitialPassword);
        AdminUser admin;
        try
        {
            admin = await _adminUsers.AddAsync(
                new AdminUser(
                    Guid.NewGuid(),
                    userName,
                    firstName,
                    lastName,
                    email,
                    legacyPasswordHash),
                cancellationToken);
        }
        catch (InvalidOperationException exception)
            when (string.Equals(exception.Message, DuplicateAdminMessage, StringComparison.Ordinal))
        {
            return FailedAdminAccess("Ya existe un admin con ese usuario o correo.");
        }

        var now = _clock.UtcNow;
        var subject = new AdminPasswordHashSubject(admin.Id);
        await _adminCredentials.UpsertAsync(
            new AdminCredential(
                admin.Id,
                _adminPasswordHasher.HashPassword(subject, command.InitialPassword),
                now,
                now),
            cancellationToken);

        return new AdminAccessResult(ToDto(admin), ErrorMessage: null);
    }

    public async Task<AdminAccessResult> ResetAdminPasswordAsync(
        ResetAdminPasswordCommand command,
        CancellationToken cancellationToken = default)
    {
        var validationError = ValidateAdminPassword(command.ActingAdminUserId, command.NewPassword);
        if (validationError is not null)
        {
            return FailedAdminAccess(validationError);
        }

        var admin = await _adminUsers.FindByIdAsync(command.TargetAdminUserId, cancellationToken);
        if (admin is null)
        {
            return FailedAdminAccess("El admin no existe.");
        }

        var now = _clock.UtcNow;
        var legacyPasswordHash = LegacyPasswordVerifier.CreateLegacyBusinessPasswordHash(command.NewPassword);
        admin = await _adminUsers.UpdatePasswordAsync(
            admin.Id,
            legacyPasswordHash,
            cancellationToken);

        var subject = new AdminPasswordHashSubject(admin.Id);
        await _adminCredentials.UpsertAsync(
            new AdminCredential(
                admin.Id,
                _adminPasswordHasher.HashPassword(subject, command.NewPassword),
                now,
                now),
            cancellationToken);

        return new AdminAccessResult(ToDto(admin), ErrorMessage: null);
    }

    public async Task<IReadOnlyList<PilotBusinessDto>> ListPilotBusinessesAsync(
        string query,
        CancellationToken cancellationToken = default)
    {
        var businesses = await _businesses.ListAsync(cancellationToken);
        var pilotRecords = await _pilotBusinesses.ListAsync(cancellationToken);
        var byBusinessId = pilotRecords.ToDictionary(record => record.BusinessId);
        var normalizedQuery = query.Trim();

        return businesses
            .Where(business => MatchesQuery(business, normalizedQuery))
            .OrderBy(business => business.Name, StringComparer.OrdinalIgnoreCase)
            .Select(business =>
            {
                byBusinessId.TryGetValue(business.Id, out var pilot);
                return ToPilotBusinessDto(business, pilot);
            })
            .ToArray();
    }

    public async Task<CreateBusinessResult> CreateBusinessAsync(
        CreateBusinessCommand command,
        CancellationToken cancellationToken = default)
    {
        var businessName = command.BusinessName.Trim();
        var businessEmail = command.BusinessEmail.Trim().ToLowerInvariant();

        var validationError = ValidateCreateBusinessCommand(
            businessName,
            businessEmail,
            command.InitialPassword,
            command.AdminUserId,
            command.Notes);
        if (validationError is not null)
        {
            return FailedCreate(validationError);
        }

        if (await _businesses.FindByNameAsync(businessName, cancellationToken) is not null)
        {
            return FailedCreate("Ya existe un negocio con ese nombre.");
        }

        if (await _businesses.FindByEmailAsync(businessEmail, cancellationToken) is not null)
        {
            return FailedCreate("Ya existe un negocio con ese correo.");
        }

        var legacyPasswordHash = LegacyPasswordVerifier.CreateLegacyBusinessPasswordHash(command.InitialPassword);
        Business business;
        try
        {
            business = await _businesses.AddAsync(
                new Business(
                    Guid.NewGuid(),
                    businessName,
                    businessEmail,
                    legacyPasswordHash,
                    DefaultBusinessLogoPath),
                cancellationToken);
        }
        catch (InvalidOperationException exception)
            when (string.Equals(
                exception.Message,
                DuplicateBusinessMessage,
                StringComparison.Ordinal))
        {
            return FailedCreate("Ya existe un negocio con ese nombre o correo.");
        }

        var now = _clock.UtcNow;
        var subject = new BusinessPasswordHashSubject(business.Id);
        await _businessCredentials.UpsertAsync(
            new BusinessCredential(
                business.Id,
                _businessPasswordHasher.HashPassword(subject, command.InitialPassword),
                now,
                now),
            cancellationToken);

        PilotBusinessAccess? access = null;
        if (command.EnablePilot)
        {
            access = new PilotBusinessAccess(
                business.Id,
                isEnabled: true,
                command.Notes,
                now,
                now,
                command.AdminUserId);
            await _pilotBusinesses.UpsertAsync(access, cancellationToken);
        }

        return new CreateBusinessResult(ToPilotBusinessDto(business, access), ErrorMessage: null);
    }

    public async Task<BusinessProfileDto?> GetBusinessProfileAsync(
        Guid businessId,
        CancellationToken cancellationToken = default)
    {
        var business = await _businesses.FindByIdAsync(businessId, cancellationToken);
        if (business is null)
        {
            return null;
        }

        var access = await _pilotBusinesses.FindByBusinessIdAsync(business.Id, cancellationToken);
        return await ToBusinessProfileDtoAsync(business, access, cancellationToken);
    }

    public async Task<BusinessProfileResult> UpdateBusinessProfileAsync(
        UpdateBusinessProfileCommand command,
        CancellationToken cancellationToken = default)
    {
        var businessName = command.BusinessName.Trim();
        var businessEmail = command.BusinessEmail.Trim().ToLowerInvariant();
        var businessLogo = NormalizeBusinessLogo(command.BusinessLogo);

        var business = await _businesses.FindByIdAsync(command.BusinessId, cancellationToken);
        if (business is null)
        {
            return FailedProfile("El negocio no existe.");
        }

        var validationError = ValidateBusinessProfile(
            businessName,
            businessEmail,
            businessLogo,
            command.AdminUserId,
            command.Notes);
        if (validationError is not null)
        {
            return FailedProfile(validationError);
        }

        var sameName = await _businesses.FindByNameAsync(businessName, cancellationToken);
        if (sameName is not null && sameName.Id != business.Id)
        {
            return FailedProfile("Ya existe un negocio con ese nombre.");
        }

        var sameEmail = await _businesses.FindByEmailAsync(businessEmail, cancellationToken);
        if (sameEmail is not null && sameEmail.Id != business.Id)
        {
            return FailedProfile("Ya existe un negocio con ese correo.");
        }

        try
        {
            business = await _businesses.UpdateAsync(
                new Business(
                    business.Id,
                    businessName,
                    businessEmail,
                    business.PasswordHashPlaceholder,
                    businessLogo),
                cancellationToken);
        }
        catch (InvalidOperationException exception)
            when (string.Equals(exception.Message, DuplicateBusinessMessage, StringComparison.Ordinal))
        {
            return FailedProfile("Ya existe un negocio con ese nombre o correo.");
        }

        var activationStatus = ResolveActivationStatus(
            command.IsPilotEnabled,
            command.ActivationStatus);
        var access = await UpsertPilotBusinessAsync(
            business.Id,
            IsModernEnabled(activationStatus),
            command.Notes,
            command.AdminUserId,
            activationStatus,
            cancellationToken);

        return new BusinessProfileResult(
            await ToBusinessProfileDtoAsync(business, access, cancellationToken),
            ErrorMessage: null);
    }

    public async Task<BusinessProfileResult> ResetBusinessPasswordAsync(
        ResetBusinessPasswordCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command.AdminUserId == Guid.Empty)
        {
            return FailedProfile("La sesion de admin no es valida.");
        }

        if (string.IsNullOrWhiteSpace(command.NewPassword))
        {
            return FailedProfile("La contrasena nueva es requerida.");
        }

        if (command.NewPassword.Length < 8)
        {
            return FailedProfile("La contrasena nueva debe tener al menos 8 caracteres.");
        }

        if (command.NewPassword.Length > 128)
        {
            return FailedProfile("La contrasena nueva no puede exceder 128 caracteres.");
        }

        var business = await _businesses.FindByIdAsync(command.BusinessId, cancellationToken);
        if (business is null)
        {
            return FailedProfile("El negocio no existe.");
        }

        var now = _clock.UtcNow;
        var legacyPasswordHash = LegacyPasswordVerifier.CreateLegacyBusinessPasswordHash(command.NewPassword);
        business = await _businesses.UpdateAsync(
            new Business(
                business.Id,
                business.Name,
                business.Email,
                legacyPasswordHash,
                business.LogoPath),
            cancellationToken);

        var subject = new BusinessPasswordHashSubject(business.Id);
        await _businessCredentials.UpsertAsync(
            new BusinessCredential(
                business.Id,
                _businessPasswordHasher.HashPassword(subject, command.NewPassword),
                now,
                now),
            cancellationToken);

        var access = await _pilotBusinesses.FindByBusinessIdAsync(business.Id, cancellationToken);
        return new BusinessProfileResult(
            await ToBusinessProfileDtoAsync(business, access, cancellationToken),
            ErrorMessage: null);
    }

    public async Task<BusinessBrandingResult> UpdateBusinessBrandingAsync(
        UpdateBusinessBrandingCommand command,
        CancellationToken cancellationToken = default)
    {
        var business = await _businesses.FindByIdAsync(command.BusinessId, cancellationToken);
        if (business is null)
        {
            return FailedBranding("El negocio no existe.");
        }

        var publicName = NormalizeBrandingValue(command.PublicName, business.Name);
        var logoPath = NormalizeBrandingValue(command.LogoPath, business.LogoPath);
        var primaryColor = NormalizeBrandingValue(command.PrimaryColor, DefaultPrimaryColor);
        var secondaryColor = NormalizeBrandingValue(command.SecondaryColor, DefaultSecondaryColor);
        var programName = NormalizeBrandingValue(command.ProgramName, DefaultProgramName);
        var programDescription = NormalizeBrandingValue(command.ProgramDescription, DefaultProgramDescription);

        var validationError = ValidateBusinessBranding(
            command.AdminUserId,
            publicName,
            logoPath,
            primaryColor,
            secondaryColor,
            programName,
            programDescription);
        if (validationError is not null)
        {
            return FailedBranding(validationError);
        }

        await _businessBranding.UpsertAsync(
            new BusinessBranding(
                business.Id,
                publicName,
                logoPath,
                primaryColor,
                secondaryColor,
                programName,
                programDescription,
                _clock.UtcNow,
                command.AdminUserId),
            cancellationToken);

        var access = await _pilotBusinesses.FindByBusinessIdAsync(business.Id, cancellationToken);
        return new BusinessBrandingResult(
            await ToBusinessProfileDtoAsync(business, access, cancellationToken),
            ErrorMessage: null);
    }

    public async Task<PilotBusinessDto?> SetPilotBusinessAsync(
        SetPilotBusinessCommand command,
        CancellationToken cancellationToken = default)
    {
        var business = await _businesses.FindByIdAsync(command.BusinessId, cancellationToken);
        if (business is null)
        {
            return null;
        }

        var access = await UpsertPilotBusinessAsync(
            command.BusinessId,
            command.ActivationStatus is null
                ? command.IsEnabled
                : IsModernEnabled(command.ActivationStatus.Value),
            command.Notes,
            command.AdminUserId,
            command.ActivationStatus,
            cancellationToken);
        return ToPilotBusinessDto(business, access);
    }

    public async Task<IReadOnlyList<PilotClientDto>> ListPilotClientsAsync(
        string query,
        CancellationToken cancellationToken = default)
    {
        var clients = await _clients.SearchAsync(query, cancellationToken);
        var pilotRecords = await _pilotClients.ListAsync(cancellationToken);
        var byClientId = pilotRecords.ToDictionary(record => record.ClientId);

        return clients
            .OrderBy(client => client.UserName, StringComparer.OrdinalIgnoreCase)
            .Select(client =>
            {
                byClientId.TryGetValue(client.Id, out var pilot);
                return ToPilotClientDto(client, pilot);
            })
            .ToArray();
    }

    public async Task<PilotClientDto?> SetPilotClientAsync(
        SetPilotClientCommand command,
        CancellationToken cancellationToken = default)
    {
        var client = await _clients.FindByIdAsync(command.ClientId, cancellationToken);
        if (client is null)
        {
            return null;
        }

        var access = await UpsertPilotClientAsync(
            command.ClientId,
            command.IsEnabled,
            command.Notes,
            command.AdminUserId,
            cancellationToken);
        return ToPilotClientDto(client, access);
    }

    public async Task<AdminSupportResult> SearchSupportAsync(
        AdminSupportQuery query,
        CancellationToken cancellationToken = default)
    {
        var normalizedQuery = NormalizeSupportQuery(query.Query);
        var normalizedBusinessFilter = NormalizeSupportQuery(query.BusinessFilter ?? string.Empty);
        var normalizedClientFilter = NormalizeSupportQuery(query.ClientFilter ?? string.Empty);
        var hasAnyFilter = query.WalletIssuesOnly ||
            query.From.HasValue ||
            query.To.HasValue ||
            !string.IsNullOrWhiteSpace(normalizedBusinessFilter) ||
            !string.IsNullOrWhiteSpace(normalizedClientFilter);
        if (string.IsNullOrWhiteSpace(normalizedQuery) && !hasAnyFilter)
        {
            return new AdminSupportResult(string.Empty, [], [], []);
        }

        var cards = new Dictionary<Guid, LoyaltyCard>();

        var exactCard = await FindSupportCardByQueryAsync(normalizedQuery, cancellationToken);
        if (exactCard is not null)
        {
            cards[exactCard.Id] = exactCard;
        }

        var clientSearchTerm = string.IsNullOrWhiteSpace(normalizedClientFilter)
            ? normalizedQuery
            : normalizedClientFilter;
        var clients = string.IsNullOrWhiteSpace(clientSearchTerm)
            ? Array.Empty<Client>()
            : (await _clients.SearchAsync(clientSearchTerm, cancellationToken))
                .Take(10)
                .ToArray();
        foreach (var client in clients)
        {
            var clientCards = await _loyaltyCards.ListByClientAsync(client.Id, cancellationToken);
            foreach (var card in clientCards)
            {
                cards.TryAdd(card.Id, card);
            }
        }

        var businesses = await _businesses.ListAsync(cancellationToken);
        var pilotBusinesses = await _pilotBusinesses.ListAsync(cancellationToken);
        var pilotByBusinessId = pilotBusinesses.ToDictionary(record => record.BusinessId);
        var businessSearchTerm = string.IsNullOrWhiteSpace(normalizedBusinessFilter)
            ? normalizedQuery
            : normalizedBusinessFilter;
        var matchingBusinesses = businesses
            .Where(business => MatchesQuery(business, businessSearchTerm))
            .OrderBy(business => business.Name, StringComparer.OrdinalIgnoreCase)
            .Take(10)
            .ToArray();

        foreach (var business in matchingBusinesses)
        {
            var businessCards = await _loyaltyCards.SearchByBusinessAsync(
                business.Id,
                query: normalizedClientFilter,
                limit: 10,
                cancellationToken);
            foreach (var card in businessCards)
            {
                cards.TryAdd(card.Id, card);
            }
        }

        var clientDtos = new List<AdminSupportClientDto>(clients.Length);
        foreach (var client in clients)
        {
            var clientCards = await _loyaltyCards.ListByClientAsync(client.Id, cancellationToken);
            clientDtos.Add(ToSupportClientDto(client, clientCards.Count));
        }

        var businessDtos = new List<AdminSupportBusinessDto>(matchingBusinesses.Length);
        foreach (var business in matchingBusinesses)
        {
            pilotByBusinessId.TryGetValue(business.Id, out var pilot);
            var recentCards = await _loyaltyCards.SearchByBusinessAsync(
                business.Id,
                query: string.Empty,
                limit: 25,
                cancellationToken);
            businessDtos.Add(ToSupportBusinessDto(business, recentCards.Count, pilot));
        }

        var cardDtos = new List<AdminSupportCardDto>();
        foreach (var card in cards.Values
            .OrderByDescending(card => card.LastStampedAt)
            .ThenBy(card => card.Id)
            .Where(card => IsWithinSupportRange(card, query.From, query.To))
            .Take(50))
        {
            var cardDto = await ToSupportCardDtoAsync(card, cancellationToken);
            if (cardDto is not null &&
                (!query.WalletIssuesOnly || cardDto.WalletIssueCount > 0))
            {
                cardDtos.Add(cardDto);
            }
        }

        return new AdminSupportResult(
            normalizedQuery,
            clientDtos,
            businessDtos,
            cardDtos.Take(25).ToArray());
    }

    public async Task<AdminReportsDto> GetReportsAsync(CancellationToken cancellationToken = default)
    {
        var businesses = await _businesses.ListAsync(cancellationToken);
        var pilotBusinesses = await _pilotBusinesses.ListAsync(cancellationToken);
        var pilotByBusinessId = pilotBusinesses.ToDictionary(record => record.BusinessId);

        var businessReports = new List<AdminReportBusinessDto>(businesses.Count);
        var reportCards = new List<AdminReportCardDto>();
        var clientIds = new HashSet<Guid>();

        foreach (var business in businesses.OrderBy(business => business.Name, StringComparer.OrdinalIgnoreCase))
        {
            var cards = await _loyaltyCards.SearchByBusinessAsync(
                business.Id,
                query: string.Empty,
                limit: 100,
                cancellationToken);
            var supportCards = new List<AdminSupportCardDto>(cards.Count);

            foreach (var card in cards)
            {
                clientIds.Add(card.ClientId);
                var supportCard = await ToSupportCardDtoAsync(card, cancellationToken);
                if (supportCard is not null)
                {
                    supportCards.Add(supportCard);
                    reportCards.Add(ToReportCardDto(supportCard));
                }
            }

            pilotByBusinessId.TryGetValue(business.Id, out var pilot);
            businessReports.Add(new AdminReportBusinessDto(
                business.Id,
                business.DisplayName,
                business.Email,
                cards.Count,
                cards.Select(card => card.ClientId).Distinct().Count(),
                cards.Sum(card => card.CurrentStamps),
                cards.Sum(card => card.LifetimeStamps),
                supportCards.Count(card => card.GoogleIssued),
                supportCards.Count(card => card.AppleTracked),
                supportCards.Sum(card => card.WalletIssueCount),
                cards.Count == 0 ? null : cards.Max(card => card.LastStampedAt),
                pilot?.IsEnabled == true));
        }

        var recentCards = reportCards
            .OrderByDescending(card => card.LastStampedAt)
            .ThenBy(card => card.BusinessName, StringComparer.OrdinalIgnoreCase)
            .Take(25)
            .ToArray();

        return new AdminReportsDto(
            businessReports.Count,
            reportCards.Count,
            clientIds.Count,
            businessReports.Sum(item => item.CurrentStampTotal),
            businessReports.Sum(item => item.LifetimeStampTotal),
            businessReports.Sum(item => item.GoogleIssuedCount),
            businessReports.Sum(item => item.AppleTrackedCount),
            businessReports.Sum(item => item.WalletIssueCount),
            businessReports,
            recentCards);
    }

    private static bool MatchesQuery(Business business, string query)
    {
        return string.IsNullOrWhiteSpace(query) ||
            business.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
            business.Email.Contains(query, StringComparison.OrdinalIgnoreCase);
    }

    private async Task<LoyaltyCard?> FindSupportCardByQueryAsync(
        string query,
        CancellationToken cancellationToken)
    {
        if (Guid.TryParse(query, out var cardId))
        {
            return await _loyaltyCards.FindByIdAsync(cardId, cancellationToken);
        }

        return await _loyaltyCards.FindByEnrollmentTokenAsync(query, cancellationToken);
    }

    private async Task<AdminSupportCardDto?> ToSupportCardDtoAsync(
        LoyaltyCard card,
        CancellationToken cancellationToken)
    {
        var client = await _clients.FindByIdAsync(card.ClientId, cancellationToken);
        var business = await _businesses.FindByIdAsync(card.BusinessId, cancellationToken);
        if (client is null || business is null)
        {
            return null;
        }

        var pilot = await _pilotBusinesses.FindByBusinessIdAsync(business.Id, cancellationToken);
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

        var recentEvents = await _stampLedger.ListRecentByCardIdAsync(card.Id, 8, cancellationToken);
        var issueCount = recentEvents.Count(HasWalletIssue);
        var legacyEvents = recentEvents
            .Where(item => item.Source == StampLedgerSource.LegacySync)
            .OrderByDescending(item => item.CreatedAt)
            .ToArray();
        var safeErrors = recentEvents
            .Where(HasWalletIssue)
            .OrderByDescending(item => item.CreatedAt)
            .Select(ToSafeSupportError)
            .Where(error => !string.IsNullOrWhiteSpace(error))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(3)
            .ToArray();

        return new AdminSupportCardDto(
            card.Id,
            ToSupportClientDto(client, CardCount: 0),
            ToSupportBusinessDto(business, RecentCardCount: 0, pilot),
            card.CurrentStamps,
            card.LifetimeStamps,
            card.CreatedAt,
            card.LastStampedAt,
            !string.IsNullOrWhiteSpace(card.GoogleObjectId),
            Suffix(card.GoogleObjectId, 8),
            applePass is not null,
            appleDeviceCount,
            applePass?.UpdatedAt,
            applePass?.UpdateTag,
            Suffix(applePass?.SerialNumber, 8),
            issueCount,
            legacyEvents.Length,
            legacyEvents.FirstOrDefault()?.CreatedAt,
            safeErrors,
            recentEvents.Select(ToStampLedgerEventDto).ToArray());
    }

    private static bool IsWithinSupportRange(
        LoyaltyCard card,
        DateTimeOffset? from,
        DateTimeOffset? to)
    {
        return (!from.HasValue || card.LastStampedAt >= from.Value) &&
            (!to.HasValue || card.LastStampedAt <= to.Value);
    }

    private static string ToSafeSupportError(StampLedgerRecord item)
    {
        if (!string.IsNullOrWhiteSpace(item.ErrorSummary))
        {
            return item.ErrorSummary.Length <= 160
                ? item.ErrorSummary
                : item.ErrorSummary[..160];
        }

        var failed = new List<string>(capacity: 2);
        if (item.GoogleWalletAttempted && !item.GoogleWalletSucceeded)
        {
            failed.Add("Google Wallet fallo");
        }

        if (item.AppleWalletAttempted && !item.AppleWalletSucceeded)
        {
            failed.Add("Apple Wallet fallo");
        }

        return string.Join(", ", failed);
    }

    private static bool HasWalletIssue(StampLedgerRecord item)
    {
        return !string.IsNullOrWhiteSpace(item.ErrorSummary) ||
            (item.GoogleWalletAttempted && !item.GoogleWalletSucceeded) ||
            (item.AppleWalletAttempted && !item.AppleWalletSucceeded);
    }

    private static AdminReportCardDto ToReportCardDto(AdminSupportCardDto card)
    {
        return new AdminReportCardDto(
            card.CardId,
            card.Business.BusinessName,
            card.Client.UserName,
            card.CurrentStamps,
            card.LifetimeStamps,
            card.LastStampedAt,
            card.GoogleIssued,
            card.AppleTracked,
            card.WalletIssueCount);
    }

    private async Task<PilotClientAccess> UpsertPilotClientAsync(
        Guid clientId,
        bool isEnabled,
        string? notes,
        Guid adminUserId,
        CancellationToken cancellationToken)
    {
        var now = _clock.UtcNow;
        var existing = await _pilotClients.FindByClientIdAsync(clientId, cancellationToken);
        var access = existing is null
            ? new PilotClientAccess(
                clientId,
                isEnabled,
                notes,
                now,
                now,
                adminUserId)
            : existing.WithState(isEnabled, notes, now, adminUserId);

        await _pilotClients.UpsertAsync(access, cancellationToken);
        return access;
    }

    private async Task<PilotBusinessAccess> UpsertPilotBusinessAsync(
        Guid businessId,
        bool isEnabled,
        string? notes,
        Guid adminUserId,
        BusinessActivationStatus? activationStatus,
        CancellationToken cancellationToken)
    {
        var now = _clock.UtcNow;
        var existing = await _pilotBusinesses.FindByBusinessIdAsync(businessId, cancellationToken);
        var access = existing is null
            ? new PilotBusinessAccess(
                businessId,
                isEnabled,
                notes,
                now,
                now,
                adminUserId,
                activationStatus)
            : existing.WithState(isEnabled, notes, now, adminUserId, activationStatus);

        await _pilotBusinesses.UpsertAsync(access, cancellationToken);
        return access;
    }

    private static string? ValidateCreateAdminCommand(
        string userName,
        string firstName,
        string lastName,
        string email,
        string initialPassword,
        Guid actingAdminUserId)
    {
        if (actingAdminUserId == Guid.Empty)
        {
            return "La sesion de admin no es valida.";
        }

        if (string.IsNullOrWhiteSpace(userName))
        {
            return "El usuario admin es requerido.";
        }

        if (userName.Length > AdminUserNameMaxLength)
        {
            return $"El usuario admin no puede exceder {AdminUserNameMaxLength} caracteres.";
        }

        if (string.IsNullOrWhiteSpace(firstName))
        {
            return "El nombre del admin es requerido.";
        }

        if (firstName.Length > AdminNameMaxLength)
        {
            return $"El nombre del admin no puede exceder {AdminNameMaxLength} caracteres.";
        }

        if (string.IsNullOrWhiteSpace(lastName))
        {
            return "El apellido del admin es requerido.";
        }

        if (lastName.Length > AdminNameMaxLength)
        {
            return $"El apellido del admin no puede exceder {AdminNameMaxLength} caracteres.";
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            return "El correo del admin es requerido.";
        }

        if (email.Length > AdminEmailMaxLength)
        {
            return $"El correo del admin no puede exceder {AdminEmailMaxLength} caracteres.";
        }

        if (!email.Contains("@", StringComparison.Ordinal))
        {
            return "El correo del admin no es valido.";
        }

        return ValidateAdminPassword(actingAdminUserId, initialPassword);
    }

    private static string? ValidateAdminPassword(Guid actingAdminUserId, string password)
    {
        if (actingAdminUserId == Guid.Empty)
        {
            return "La sesion de admin no es valida.";
        }

        if (string.IsNullOrWhiteSpace(password))
        {
            return "La contrasena de admin es requerida.";
        }

        if (password.Length < 8)
        {
            return "La contrasena de admin debe tener al menos 8 caracteres.";
        }

        if (password.Length > 128)
        {
            return "La contrasena de admin no puede exceder 128 caracteres.";
        }

        return null;
    }

    private static string? ValidateCreateBusinessCommand(
        string businessName,
        string businessEmail,
        string initialPassword,
        Guid adminUserId,
        string? notes)
    {
        if (adminUserId == Guid.Empty)
        {
            return "La sesion de admin no es valida.";
        }

        if (string.IsNullOrWhiteSpace(businessName))
        {
            return "El nombre del negocio es requerido.";
        }

        if (businessName.Length > BusinessNameMaxLength)
        {
            return $"El nombre del negocio no puede exceder {BusinessNameMaxLength} caracteres.";
        }

        if (string.IsNullOrWhiteSpace(businessEmail))
        {
            return "El correo del negocio es requerido.";
        }

        if (businessEmail.Length > BusinessEmailMaxLength)
        {
            return $"El correo del negocio no puede exceder {BusinessEmailMaxLength} caracteres.";
        }

        if (!businessEmail.Contains("@", StringComparison.Ordinal))
        {
            return "El correo del negocio no es valido.";
        }

        if (string.IsNullOrWhiteSpace(initialPassword))
        {
            return "La contrasena inicial es requerida.";
        }

        if (initialPassword.Length < 8)
        {
            return "La contrasena inicial debe tener al menos 8 caracteres.";
        }

        if (notes?.Length > NotesMaxLength)
        {
            return $"Las notas no pueden exceder {NotesMaxLength} caracteres.";
        }

        return null;
    }

    private static string? ValidateBusinessProfile(
        string businessName,
        string businessEmail,
        string businessLogo,
        Guid adminUserId,
        string? notes)
    {
        if (adminUserId == Guid.Empty)
        {
            return "La sesion de admin no es valida.";
        }

        if (string.IsNullOrWhiteSpace(businessName))
        {
            return "El nombre del negocio es requerido.";
        }

        if (businessName.Length > BusinessNameMaxLength)
        {
            return $"El nombre del negocio no puede exceder {BusinessNameMaxLength} caracteres.";
        }

        if (string.IsNullOrWhiteSpace(businessEmail))
        {
            return "El correo del negocio es requerido.";
        }

        if (businessEmail.Length > BusinessEmailMaxLength)
        {
            return $"El correo del negocio no puede exceder {BusinessEmailMaxLength} caracteres.";
        }

        if (!businessEmail.Contains("@", StringComparison.Ordinal))
        {
            return "El correo del negocio no es valido.";
        }

        if (businessLogo.Length > BusinessLogoMaxLength)
        {
            return $"El logo del negocio no puede exceder {BusinessLogoMaxLength} caracteres.";
        }

        if (notes?.Length > NotesMaxLength)
        {
            return $"Las notas no pueden exceder {NotesMaxLength} caracteres.";
        }

        return null;
    }

    private static string? ValidateBusinessBranding(
        Guid adminUserId,
        string publicName,
        string logoPath,
        string primaryColor,
        string secondaryColor,
        string programName,
        string programDescription)
    {
        if (adminUserId == Guid.Empty)
        {
            return "La sesion de admin no es valida.";
        }

        if (string.IsNullOrWhiteSpace(publicName))
        {
            return "El nombre publico es requerido.";
        }

        if (publicName.Length > BrandingNameMaxLength)
        {
            return $"El nombre publico no puede exceder {BrandingNameMaxLength} caracteres.";
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

    private static CreateBusinessResult FailedCreate(string errorMessage)
    {
        return new CreateBusinessResult(null, errorMessage);
    }

    private static AdminAccessResult FailedAdminAccess(string errorMessage)
    {
        return new AdminAccessResult(null, errorMessage);
    }

    private static BusinessProfileResult FailedProfile(string errorMessage)
    {
        return new BusinessProfileResult(null, errorMessage);
    }

    private static BusinessBrandingResult FailedBranding(string errorMessage)
    {
        return new BusinessBrandingResult(null, errorMessage);
    }

    private static string NormalizeBusinessLogo(string businessLogo)
    {
        return string.IsNullOrWhiteSpace(businessLogo)
            ? DefaultBusinessLogoPath
            : businessLogo.Trim();
    }

    private static string NormalizeBrandingValue(string value, string fallback)
    {
        return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
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

    private static AdminUserDto ToDto(AdminUser admin)
    {
        return new AdminUserDto(
            admin.Id,
            admin.UserName,
            string.IsNullOrWhiteSpace(admin.FullName) ? admin.UserName : admin.FullName,
            admin.Email);
    }

    private static AdminUserListItemDto ToListItemDto(AdminUser admin)
    {
        return new AdminUserListItemDto(
            admin.Id,
            admin.UserName,
            string.IsNullOrWhiteSpace(admin.FullName) ? admin.UserName : admin.FullName,
            admin.Email);
    }

    private static PilotBusinessDto ToPilotBusinessDto(Business business, PilotBusinessAccess? access)
    {
        return new PilotBusinessDto(
            business.Id,
            business.Name,
            business.Email,
            access?.IsEnabled ?? false,
            access?.ActivationStatus ?? BusinessActivationStatus.LegacyOnly,
            access?.Notes,
            access?.UpdatedAt);
    }

    private async Task<BusinessProfileDto> ToBusinessProfileDtoAsync(
        Business business,
        PilotBusinessAccess? access,
        CancellationToken cancellationToken)
    {
        var branding = await _businessBranding.FindByBusinessIdAsync(business.Id, cancellationToken);

        return new BusinessProfileDto(
            business.Id,
            business.Name,
            business.Email,
            business.LogoPath,
            access?.IsEnabled ?? false,
            access?.ActivationStatus ?? BusinessActivationStatus.LegacyOnly,
            access?.Notes,
            access?.UpdatedAt,
            ToBrandingDto(business, branding));
    }

    private static BusinessActivationStatus ResolveActivationStatus(
        bool isPilotEnabled,
        BusinessActivationStatus? requestedStatus)
    {
        return requestedStatus ?? (isPilotEnabled
            ? BusinessActivationStatus.PilotModern
            : BusinessActivationStatus.LegacyOnly);
    }

    private static bool IsModernEnabled(BusinessActivationStatus activationStatus)
    {
        return activationStatus is not BusinessActivationStatus.LegacyOnly
            and not BusinessActivationStatus.Inactive;
    }

    private static BusinessBrandingDto ToBrandingDto(Business business, BusinessBranding? branding)
    {
        return new BusinessBrandingDto(
            branding?.PublicName ?? business.Name,
            branding?.LogoPath ?? business.LogoPath,
            branding?.PrimaryColor ?? DefaultPrimaryColor,
            branding?.SecondaryColor ?? DefaultSecondaryColor,
            branding?.ProgramName ?? DefaultProgramName,
            branding?.ProgramDescription ?? DefaultProgramDescription,
            branding?.UpdatedAt);
    }

    private static PilotClientDto ToPilotClientDto(Client client, PilotClientAccess? access)
    {
        return new PilotClientDto(
            client.Id,
            client.UserName,
            client.FullName,
            client.Email,
            access?.IsEnabled ?? false,
            access?.Notes,
            access?.UpdatedAt);
    }

    private static AdminSupportClientDto ToSupportClientDto(Client client, int CardCount)
    {
        return new AdminSupportClientDto(
            client.Id,
            client.UserName,
            client.FullName,
            client.Email,
            CardCount);
    }

    private static AdminSupportBusinessDto ToSupportBusinessDto(
        Business business,
        int RecentCardCount,
        PilotBusinessAccess? pilot)
    {
        return new AdminSupportBusinessDto(
            business.Id,
            business.Name,
            business.Email,
            RecentCardCount,
            pilot?.IsEnabled ?? false,
            pilot?.ActivationStatus ?? BusinessActivationStatus.LegacyOnly);
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

    private static string NormalizeSupportQuery(string query)
    {
        var normalized = query.Trim();
        if (Uri.TryCreate(normalized, UriKind.Absolute, out var uri))
        {
            normalized = uri.Segments.LastOrDefault()?.Trim('/') ?? normalized;
        }

        return normalized.EndsWith(".pkpass", StringComparison.OrdinalIgnoreCase)
            ? normalized[..^".pkpass".Length]
            : normalized;
    }

    private static string? Suffix(string? value, int length)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Trim();
        return normalized.Length <= length ? normalized : normalized[^length..];
    }
}
