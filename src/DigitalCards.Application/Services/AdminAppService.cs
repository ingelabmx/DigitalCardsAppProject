using DigitalCards.Application.Abstractions;
using DigitalCards.Application.Models;
using DigitalCards.Domain;

namespace DigitalCards.Application.Services;

public sealed class AdminAppService
{
    private readonly IAdminUserRepository _adminUsers;
    private readonly IBusinessRepository _businesses;
    private readonly IClock _clock;
    private readonly IPilotBusinessRepository _pilotBusinesses;

    public AdminAppService(
        IAdminUserRepository adminUsers,
        IBusinessRepository businesses,
        IPilotBusinessRepository pilotBusinesses,
        IClock clock)
    {
        _adminUsers = adminUsers;
        _businesses = businesses;
        _pilotBusinesses = pilotBusinesses;
        _clock = clock;
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

    public async Task<PilotBusinessDto?> SetPilotBusinessAsync(
        SetPilotBusinessCommand command,
        CancellationToken cancellationToken = default)
    {
        var business = await _businesses.FindByIdAsync(command.BusinessId, cancellationToken);
        if (business is null)
        {
            return null;
        }

        var now = _clock.UtcNow;
        var existing = await _pilotBusinesses.FindByBusinessIdAsync(command.BusinessId, cancellationToken);
        var access = existing is null
            ? new PilotBusinessAccess(
                command.BusinessId,
                command.IsEnabled,
                command.Notes,
                now,
                now,
                command.AdminUserId)
            : existing.WithState(command.IsEnabled, command.Notes, now, command.AdminUserId);

        await _pilotBusinesses.UpsertAsync(access, cancellationToken);
        return ToPilotBusinessDto(business, access);
    }

    private static bool MatchesQuery(Business business, string query)
    {
        return string.IsNullOrWhiteSpace(query) ||
            business.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
            business.Email.Contains(query, StringComparison.OrdinalIgnoreCase);
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
}
