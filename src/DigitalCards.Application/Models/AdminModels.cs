namespace DigitalCards.Application.Models;

public sealed record AdminLoginCommand(string UserNameOrEmail, string Password);

public sealed record AdminUserDto(
    Guid Id,
    string UserName,
    string Name,
    string Email);

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
