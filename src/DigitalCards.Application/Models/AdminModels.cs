using DigitalCards.Domain;

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
    BusinessActivationStatus ActivationStatus,
    string? Notes,
    DateTimeOffset? PilotUpdatedAt,
    BusinessBrandingDto Branding);

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
    string? Notes,
    BusinessActivationStatus? ActivationStatus = null);

public sealed record ResetBusinessPasswordCommand(
    Guid BusinessId,
    Guid AdminUserId,
    string NewPassword);

public sealed record BusinessBrandingDto(
    string PublicName,
    string LogoPath,
    string PrimaryColor,
    string SecondaryColor,
    string ProgramName,
    string ProgramDescription,
    DateTimeOffset? UpdatedAt);

public sealed record BusinessBrandingResult(
    BusinessProfileDto? Business,
    string? ErrorMessage)
{
    public bool Succeeded => Business is not null;
}

public sealed record UpdateBusinessBrandingCommand(
    Guid BusinessId,
    Guid AdminUserId,
    string PublicName,
    string LogoPath,
    string PrimaryColor,
    string SecondaryColor,
    string ProgramName,
    string ProgramDescription);

public sealed record PilotBusinessDto(
    Guid BusinessId,
    string BusinessName,
    string BusinessEmail,
    bool IsEnabled,
    BusinessActivationStatus ActivationStatus,
    string? Notes,
    DateTimeOffset? UpdatedAt);

public sealed record SetPilotBusinessCommand(
    Guid BusinessId,
    Guid AdminUserId,
    bool IsEnabled,
    string? Notes,
    BusinessActivationStatus? ActivationStatus = null);

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

public sealed record AdminSupportQuery(string Query);

public sealed record AdminSupportResult(
    string Query,
    IReadOnlyList<AdminSupportClientDto> Clients,
    IReadOnlyList<AdminSupportBusinessDto> Businesses,
    IReadOnlyList<AdminSupportCardDto> Cards);

public sealed record AdminSupportClientDto(
    Guid ClientId,
    string UserName,
    string ClientName,
    string ClientEmail,
    int CardCount);

public sealed record AdminSupportBusinessDto(
    Guid BusinessId,
    string BusinessName,
    string BusinessEmail,
    int RecentCardCount,
    bool IsPilotEnabled);

public sealed record AdminSupportCardDto(
    Guid CardId,
    AdminSupportClientDto Client,
    AdminSupportBusinessDto Business,
    int CurrentStamps,
    int LifetimeStamps,
    DateTimeOffset CreatedAt,
    DateTimeOffset LastStampedAt,
    bool GoogleIssued,
    string? GoogleObjectSuffix,
    bool AppleTracked,
    int AppleRegisteredDeviceCount,
    DateTimeOffset? AppleUpdatedAt,
    string? AppleUpdateTag,
    string? AppleSerialSuffix,
    int WalletIssueCount,
    IReadOnlyList<StampLedgerEventDto> RecentStampEvents);

public sealed record AdminReportsDto(
    int BusinessCount,
    int CardCount,
    int ClientCount,
    int CurrentStampTotal,
    int LifetimeStampTotal,
    int GoogleIssuedCount,
    int AppleTrackedCount,
    int WalletIssueCount,
    IReadOnlyList<AdminReportBusinessDto> Businesses,
    IReadOnlyList<AdminReportCardDto> RecentCards);

public sealed record AdminReportBusinessDto(
    Guid BusinessId,
    string BusinessName,
    string BusinessEmail,
    int CardCount,
    int ClientCount,
    int CurrentStampTotal,
    int LifetimeStampTotal,
    int GoogleIssuedCount,
    int AppleTrackedCount,
    int WalletIssueCount,
    DateTimeOffset? LastStampedAt,
    bool IsPilotEnabled);

public sealed record AdminReportCardDto(
    Guid CardId,
    string BusinessName,
    string ClientUserName,
    int CurrentStamps,
    int LifetimeStamps,
    DateTimeOffset LastStampedAt,
    bool GoogleIssued,
    bool AppleTracked,
    int WalletIssueCount);
