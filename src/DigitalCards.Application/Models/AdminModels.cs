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

public sealed record DeleteBusinessCommand(
    Guid BusinessId,
    Guid AdminUserId,
    string Confirmation);

public sealed record DeleteBusinessResult(
    bool Succeeded,
    string? ErrorMessage);

public sealed record BusinessBrandingDto(
    string PublicName,
    string LogoPath,
    string PrimaryColor,
    string SecondaryColor,
    string CustomFieldColor,
    int StampGoal,
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
    string CustomFieldColor,
    int StampGoal,
    string ProgramName,
    string ProgramDescription);

public sealed record PilotBusinessDto(
    Guid BusinessId,
    string BusinessName,
    string BusinessEmail,
    bool IsEnabled,
    BusinessActivationStatus ActivationStatus,
    int ClientCount,
    int CurrentStampTotal,
    string? Notes,
    DateTimeOffset? UpdatedAt,
    string? SubscriptionStatus = null,
    string? StripePlanKey = null,
    DateTimeOffset? GraceEndsAt = null);

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

public sealed record AdminClientConsoleDto(
    Guid ClientId,
    string UserName,
    string ClientName,
    string ClientEmail,
    IReadOnlyList<string> LinkedBusinessNames,
    int CardCount,
    int CurrentStamps,
    int LifetimeStamps,
    DateTimeOffset? LastActivityAt,
    IReadOnlyList<AdminClientCardConsoleDto> Cards);

public sealed record AdminClientCardConsoleDto(
    Guid CardId,
    Guid BusinessId,
    string BusinessName,
    int CurrentStamps,
    int LifetimeStamps,
    DateTimeOffset LastActivityAt,
    string CardStatus);

public sealed record SetPilotClientCommand(
    Guid ClientId,
    Guid AdminUserId,
    bool IsEnabled,
    string? Notes);

public sealed record DeleteClientCommand(
    Guid ClientId,
    Guid AdminUserId,
    string Confirmation);

public sealed record DeleteClientResult(
    bool Succeeded,
    string? ErrorMessage);

public sealed record AdminSupportQuery(
    string Query,
    string? BusinessFilter = null,
    string? ClientFilter = null,
    bool WalletIssuesOnly = false,
    DateTimeOffset? From = null,
    DateTimeOffset? To = null);

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
    bool IsPilotEnabled,
    BusinessActivationStatus ActivationStatus);

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
    int LegacySyncEventCount,
    DateTimeOffset? LastLegacySyncAt,
    IReadOnlyList<string> RecentSafeErrors,
    IReadOnlyList<StampLedgerEventDto> RecentStampEvents,
    IReadOnlyList<RewardRedemptionDto> RecentRewardRedemptions);

public sealed record AdminWalletRetryCommand(
    Guid CardId,
    Guid AdminUserId);

public sealed record AdminWalletRetryResult(
    AdminSupportCardDto? Card,
    string? ErrorMessage)
{
    public bool Succeeded => Card is not null && ErrorMessage is null;
}

public sealed record AdminWalletBrandingRefreshCommand(
    Guid BusinessId,
    Guid AdminUserId,
    int Limit = 25);

public sealed record AdminWalletBrandingRefreshResult(
    WalletBrandingRefreshResult? Refresh,
    string? ErrorMessage)
{
    public bool Succeeded => Refresh is not null && ErrorMessage is null && Refresh.ErrorMessage is null;
}

public sealed record CutoverSmokeEvidenceDto(
    long Id,
    Guid BusinessId,
    Guid AdminUserId,
    string AdminUserName,
    bool HealthOk,
    bool ReadyOk,
    bool EmailOk,
    bool AppleWalletOk,
    bool GoogleWalletOk,
    bool ModernStampOk,
    bool SupportReviewed,
    bool IsComplete,
    string? Notes,
    DateTimeOffset CreatedAt);

public sealed record RecordCutoverSmokeEvidenceCommand(
    Guid BusinessId,
    Guid AdminUserId,
    bool HealthOk,
    bool ReadyOk,
    bool EmailOk,
    bool AppleWalletOk,
    bool GoogleWalletOk,
    bool ModernStampOk,
    bool SupportReviewed,
    string? Notes);

public sealed record RecordCutoverSmokeEvidenceResult(
    CutoverSmokeEvidenceDto? Evidence,
    string? ErrorMessage)
{
    public bool Succeeded => Evidence is not null && ErrorMessage is null;
}

public sealed record AdminReportsDto(
    int BusinessCount,
    int CardCount,
    int ClientCount,
    int CurrentStampTotal,
    int LifetimeStampTotal,
    int WalletReadyCount,
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
    int WalletReadyCount,
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
