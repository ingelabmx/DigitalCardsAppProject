namespace DigitalCards.Application.Models;

public sealed record AdminLoginCommand(string UserNameOrEmail, string Password);

public sealed record AdminUserDto(
    Guid Id,
    string UserName,
    string Name,
    string Email);

public sealed record AdminUserListItemDto(
    Guid Id,
    string UserName,
    string Name,
    string Email);

public sealed record AdminAccessResult(
    AdminUserDto? Admin,
    string? ErrorMessage)
{
    public bool Succeeded => Admin is not null;
}

public sealed record CreateAdminCommand(
    string UserName,
    string FirstName,
    string LastName,
    string Email,
    string InitialPassword,
    Guid ActingAdminUserId);

public sealed record ResetAdminPasswordCommand(
    Guid TargetAdminUserId,
    Guid ActingAdminUserId,
    string NewPassword);

public sealed record CreateBusinessCommand(
    string BusinessName,
    string BusinessEmail,
    string InitialPassword,
    Guid AdminUserId,
    bool EnablePilot,
    string? Notes);

public sealed record CreateBusinessResult(
    PilotBusinessDto? Business,
    string? ErrorMessage)
{
    public bool Succeeded => Business is not null;
}

public sealed record BusinessProfileDto(
    Guid BusinessId,
    string BusinessName,
    string BusinessEmail,
    string BusinessLogo,
    bool IsPilotEnabled,
    string? Notes,
    DateTimeOffset? PilotUpdatedAt);

public sealed record BusinessProfileResult(
    BusinessProfileDto? Business,
    string? ErrorMessage)
{
    public bool Succeeded => Business is not null;
}

public sealed record UpdateBusinessProfileCommand(
    Guid BusinessId,
    Guid AdminUserId,
    string BusinessName,
    string BusinessEmail,
    string BusinessLogo,
    bool IsPilotEnabled,
    string? Notes);

public sealed record ResetBusinessPasswordCommand(
    Guid BusinessId,
    Guid AdminUserId,
    string NewPassword);

public sealed record PilotBusinessDto(
    Guid BusinessId,
    string BusinessName,
    string BusinessEmail,
    bool IsEnabled,
    string? Notes,
    DateTimeOffset? UpdatedAt);

public sealed record SetPilotBusinessCommand(
    Guid BusinessId,
    Guid AdminUserId,
    bool IsEnabled,
    string? Notes);

public sealed record PilotClientDto(
    Guid ClientId,
    string UserName,
    string ClientName,
    string ClientEmail,
    bool IsEnabled,
    string? Notes,
    DateTimeOffset? UpdatedAt);

public sealed record SetPilotClientCommand(
    Guid ClientId,
    Guid AdminUserId,
    bool IsEnabled,
    string? Notes);
