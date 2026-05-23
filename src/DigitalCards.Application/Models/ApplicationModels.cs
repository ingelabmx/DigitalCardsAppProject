namespace DigitalCards.Application.Models;

public sealed record RegisterClientCommand(
    string UserName,
    string FirstName,
    string LastName,
    string Email,
    string Password = "");

public sealed record ClientLoginCommand(string UserNameOrEmail, string Password);

public sealed record ChangeClientPasswordCommand(Guid ClientId, string CurrentPassword, string NewPassword);

public sealed record ChangeClientPasswordResult(bool Succeeded, string? ErrorMessage);

public sealed record UpdateClientProfileCommand(
    Guid ClientId,
    string FirstName,
    string LastName,
    string Email);

public sealed record UpdateClientProfileResult(ClientDto? Client, string? ErrorMessage)
{
    public bool Succeeded => Client is not null;
}

public sealed record BusinessLoginCommand(string Email, string Password);

public sealed record ChangeBusinessPasswordCommand(Guid BusinessId, string CurrentPassword, string NewPassword);

public sealed record ChangeBusinessPasswordResult(bool Succeeded, string? ErrorMessage);

public sealed record RequestClientPasswordResetCommand(string UserNameOrEmail, string BaseUrl);

public sealed record RequestBusinessPasswordResetCommand(string Email, string BaseUrl);

public sealed record PasswordResetRequestResult(bool Accepted);

public sealed record ResetPasswordCommand(string Token, string NewPassword);

public sealed record ResetPasswordResult(bool Succeeded, string? ErrorMessage);

public sealed record EnrollClientCommand(Guid BusinessId, string UserNameOrEmail, string BaseUrl);

public sealed record AddStampCommand(Guid BusinessId, string UserNameOrEmail);

public sealed record ClientDto(Guid Id, string UserName, string FirstName, string LastName, string Email);

public sealed record BusinessDto(Guid Id, string Name, string Email, string LogoPath);

public sealed record BusinessBrandingSettingsDto(
    Guid BusinessId,
    string BusinessName,
    string BusinessEmail,
    BusinessBrandingDto Branding);

public sealed record BusinessSelfServiceBrandingResult(
    BusinessBrandingSettingsDto? Settings,
    string? ErrorMessage)
{
    public bool Succeeded => Settings is not null;
}

public sealed record UpdateBusinessSelfServiceBrandingCommand(
    Guid BusinessId,
    string PublicName,
    string LogoPath,
    string PrimaryColor,
    string SecondaryColor,
    string CustomFieldColor,
    int StampGoal,
    string ProgramName,
    string ProgramDescription);

public sealed record WalletBrandingRefreshCommand(Guid BusinessId, int Limit = 25);

public sealed record WalletBrandingRefreshResult(
    Guid BusinessId,
    int CardsScanned,
    int CardsWithTrackedWallets,
    int GoogleWalletAttempted,
    int GoogleWalletSucceeded,
    int AppleWalletAttempted,
    int AppleWalletSucceeded,
    IReadOnlyList<string> SafeErrors,
    string? ErrorMessage)
{
    public bool Succeeded => ErrorMessage is null && SafeErrors.Count == 0;

    public bool HasWarnings => SafeErrors.Count > 0;
}

public sealed record LoyaltyCardDto(
    Guid Id,
    string EnrollmentToken,
    string BusinessName,
    string ClientUserName,
    int CurrentStamps,
    int StampGoal,
    int LifetimeStamps,
    int VisibleStamps,
    bool RewardReady,
    int StampsRemaining,
    string? GoogleObjectId,
    string? GoogleSaveUrl);

public sealed record ClientLoyaltyCardDto(
    Guid Id,
    string WalletSelectToken,
    string BusinessName,
    string ClientUserName,
    int CurrentStamps,
    int StampGoal,
    int LifetimeStamps,
    DateTimeOffset LastStampedAt,
    bool GoogleIssued,
    string? GoogleSaveUrl,
    bool AppleTracked,
    int AppleRegisteredDeviceCount,
    DateTimeOffset? AppleUpdatedAt,
    IReadOnlyList<RewardRedemptionDto> RecentRewardRedemptions,
    string? PrimaryColor,
    string? SecondaryColor);

public sealed record ClientDashboardDto(
    ClientDto Client,
    IReadOnlyList<ClientLoyaltyCardDto> Cards,
    int TotalCurrentStamps,
    int TotalLifetimeStamps,
    int GoogleIssuedCount,
    int AppleTrackedCount);

public sealed record EnrollClientResult(LoyaltyCardDto Card, string EnrollmentUrl);

public sealed record BusinessCardDto(
    Guid Id,
    string EnrollmentToken,
    ClientDto Client,
    string BusinessName,
    DateTimeOffset CreatedAt,
    int CurrentStamps,
    int StampGoal,
    int LifetimeStamps,
    int VisibleStamps,
    bool RewardReady,
    int StampsRemaining,
    DateTimeOffset LastStampedAt,
    bool GoogleIssued,
    bool AppleTracked,
    int AppleRegisteredDeviceCount,
    DateTimeOffset? AppleUpdatedAt,
    bool IsActive,
    IReadOnlyList<StampLedgerEventDto> RecentStampEvents,
    IReadOnlyList<RewardRedemptionDto> RecentRewardRedemptions);

public sealed record ResendWalletEmailResult(BusinessCardDto Card, string EnrollmentUrl);

public sealed record RewardRedemptionResult(
    bool Succeeded,
    BusinessCardDto? Card,
    RewardRedemptionDto? Redemption,
    string? ErrorMessage,
    bool HasWalletWarning);

public sealed record ClientCardLifecycleRecord(
    Guid CardId,
    Guid BusinessId,
    bool IsActive,
    DateTimeOffset UpdatedAt,
    Guid? UpdatedByBusinessId);

public sealed record BusinessCardLifecycleResult(
    bool Succeeded,
    BusinessCardDto? Card,
    string? ErrorMessage);

public sealed record BusinessDashboardDto(
    BusinessDto Business,
    int RecentCardCount,
    int CurrentStampTotal,
    int LifetimeStampTotal,
    int GoogleIssuedCount,
    int AppleTrackedCount,
    int AppleRegisteredDeviceCount,
    int WalletIssueCount,
    IReadOnlyList<BusinessCardDto> RecentCards,
    IReadOnlyList<BusinessDashboardStampEventDto> RecentStampEvents);

public sealed record BusinessDashboardStampEventDto(
    Guid CardId,
    string ClientUserName,
    string ClientName,
    DateTimeOffset CreatedAt,
    StampLedgerSource Source,
    int PreviousCheckQTY,
    int NewCheckQTY,
    bool GoogleWalletAttempted,
    bool GoogleWalletSucceeded,
    bool AppleWalletAttempted,
    bool AppleWalletSucceeded,
    string? ErrorSummary);

public sealed record BusinessReportsDto(
    BusinessDto Business,
    int CardCount,
    int CardsCreatedLast30Days,
    int ClientCount,
    int CurrentStampTotal,
    int LifetimeStampTotal,
    int StampsLast30Days,
    int WalletReadyCount,
    int WalletPendingCount,
    int GoogleIssuedCount,
    int GooglePendingCount,
    int AppleTrackedCount,
    int ApplePendingCount,
    int AppleRegisteredDeviceCount,
    int WalletIssueCount,
    int RedemptionsLast30Days,
    decimal RedemptionRate,
    IReadOnlyList<BusinessReportPeriodDto> StampPeriods,
    IReadOnlyList<BusinessReportPeriodDto> NewClientPeriods,
    IReadOnlyList<BusinessReportClientDto> RecentClients,
    IReadOnlyList<BusinessReportTopClientDto> TopClients,
    IReadOnlyList<BusinessDashboardStampEventDto> RecentWalletIssues);

public sealed record BusinessReportPeriodDto(
    string Period,
    int StampCount,
    int RedemptionCount = 0,
    int NewClientCount = 0);

public sealed record BusinessReportClientDto(
    Guid ClientId,
    string UserName,
    string ClientName,
    string Email,
    int CardCount,
    int CurrentStamps,
    int LifetimeStamps,
    string CardStatus,
    DateTimeOffset LastActivityAt);

public sealed record BusinessReportTopClientDto(
    Guid ClientId,
    string UserName,
    string ClientName,
    int CurrentStamps,
    int LifetimeStamps,
    int RedemptionCount,
    DateTimeOffset LastActivityAt);

public sealed record WalletLandingDto(
    string Token,
    string BusinessName,
    string ClientName,
    string ClientUserName,
    int CurrentStamps,
    int StampGoal,
    int LifetimeStamps,
    bool HasGooglePass,
    string LogoPath,
    string? PrimaryColor,
    string? SecondaryColor,
    string? CustomFieldColor,
    string? ProgramName,
    string? RewardText);

public sealed record GoogleWalletIssueResult(string ObjectId, string SaveUrl);

public enum AppleWalletIssueStatus
{
    Pending,
    Ready
}

public sealed record AppleWalletIssueResult(
    AppleWalletIssueStatus Status,
    string Message,
    string? DownloadUrl,
    string? SerialNumber);

public sealed record AppleWalletPassFile(
    byte[] Content,
    string ContentType,
    string FileName,
    string SerialNumber,
    DateTimeOffset LastModified);

public sealed record WalletEnrollmentEmail(
    string To,
    string ClientName,
    string BusinessName,
    string EnrollmentUrl,
    DateTimeOffset CreatedAt,
    string? BusinessLogoUrl = null,
    string? PrimaryColor = null,
    string? ProgramName = null);
