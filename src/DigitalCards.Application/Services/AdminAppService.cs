using DigitalCards.Application.Abstractions;
using DigitalCards.Application.Models;
using DigitalCards.Domain;
using Microsoft.AspNetCore.Identity;

namespace DigitalCards.Application.Services;

public sealed class AdminAppService
{
    private const int BusinessNameMaxLength = 30;
    private const int BusinessEmailMaxLength = 30;
    private const int BusinessLogoMaxLength = 100;
    private const int NotesMaxLength = 500;
    private const string DefaultBusinessLogoPath = "/img/demo-coffee.svg";
    private const string DuplicateBusinessMessage = "A business with the same name or email already exists.";

    private readonly IAdminUserRepository _adminUsers;
    private readonly IBusinessCredentialRepository _businessCredentials;
    private readonly IBusinessRepository _businesses;
    private readonly IClock _clock;
    private readonly IPasswordHasher<BusinessPasswordHashSubject> _passwordHasher;
    private readonly IPilotBusinessRepository _pilotBusinesses;

    public AdminAppService(
        IAdminUserRepository adminUsers,
        IBusinessRepository businesses,
        IBusinessCredentialRepository businessCredentials,
        IPilotBusinessRepository pilotBusinesses,
        IClock clock,
        IPasswordHasher<BusinessPasswordHashSubject> passwordHasher)
    {
        _adminUsers = adminUsers;
        _businesses = businesses;
        _businessCredentials = businessCredentials;
        _pilotBusinesses = pilotBusinesses;
        _clock = clock;
        _passwordHasher = passwordHasher;
    }

    public async Task<AdminUserDto?> LoginAdminAsync(
        AdminLoginCommand command,
        CancellationToken cancellationToken = default)
    {
        var admin = await _adminUsers.FindByUserNameOrEmailAsync(
            command.UserNameOrEmail,
            cancellationToken);

        if (admin is null ||
            !LegacyPasswordVerifier.Matches(admin.PasswordHashPlaceholder, command.Password))
        {
            return null;
        }

        return ToDto(admin);
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
                _passwordHasher.HashPassword(subject, command.InitialPassword),
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
        return ToBusinessProfileDto(business, access);
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

        var access = await UpsertPilotBusinessAsync(
            business.Id,
            command.IsPilotEnabled,
            command.Notes,
            command.AdminUserId,
            cancellationToken);

        return new BusinessProfileResult(ToBusinessProfileDto(business, access), ErrorMessage: null);
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
                _passwordHasher.HashPassword(subject, command.NewPassword),
                now,
                now),
            cancellationToken);

        var access = await _pilotBusinesses.FindByBusinessIdAsync(business.Id, cancellationToken);
        return new BusinessProfileResult(ToBusinessProfileDto(business, access), ErrorMessage: null);
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
            command.IsEnabled,
            command.Notes,
            command.AdminUserId,
            cancellationToken);
        return ToPilotBusinessDto(business, access);
    }

    private static bool MatchesQuery(Business business, string query)
    {
        return string.IsNullOrWhiteSpace(query) ||
            business.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
            business.Email.Contains(query, StringComparison.OrdinalIgnoreCase);
    }

    private async Task<PilotBusinessAccess> UpsertPilotBusinessAsync(
        Guid businessId,
        bool isEnabled,
        string? notes,
        Guid adminUserId,
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
                adminUserId)
            : existing.WithState(isEnabled, notes, now, adminUserId);

        await _pilotBusinesses.UpsertAsync(access, cancellationToken);
        return access;
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

    private static CreateBusinessResult FailedCreate(string errorMessage)
    {
        return new CreateBusinessResult(null, errorMessage);
    }

    private static BusinessProfileResult FailedProfile(string errorMessage)
    {
        return new BusinessProfileResult(null, errorMessage);
    }

    private static string NormalizeBusinessLogo(string businessLogo)
    {
        return string.IsNullOrWhiteSpace(businessLogo)
            ? DefaultBusinessLogoPath
            : businessLogo.Trim();
    }

    private static AdminUserDto ToDto(AdminUser admin)
    {
        return new AdminUserDto(
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
            access?.Notes,
            access?.UpdatedAt);
    }

    private static BusinessProfileDto ToBusinessProfileDto(Business business, PilotBusinessAccess? access)
    {
        return new BusinessProfileDto(
            business.Id,
            business.Name,
            business.Email,
            business.LogoPath,
            access?.IsEnabled ?? false,
            access?.Notes,
            access?.UpdatedAt);
    }
}
