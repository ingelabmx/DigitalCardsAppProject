namespace DigitalCards.Application.Models;

public enum StampLedgerSource
{
    ModernBusiness,
    LegacySync,
    AdminRetry,
    BrandingRefresh,
    RewardRedeemed
}

public sealed record StampLedgerRecord(
    long Id,
    Guid CardId,
    Guid BusinessId,
    Guid UserId,
    StampLedgerSource Source,
    Guid? ActorBusinessId,
    int PreviousCheckQTY,
    int NewCheckQTY,
    int PreviousHistoricCheckQTY,
    int NewHistoricCheckQTY,
    DateTimeOffset ObservedLastCheck,
    bool GoogleWalletAttempted,
    bool GoogleWalletSucceeded,
    bool AppleWalletAttempted,
    bool AppleWalletSucceeded,
    string? ErrorSummary,
    DateTimeOffset CreatedAt);

public sealed record StampLedgerEventDto(
    DateTimeOffset CreatedAt,
    StampLedgerSource Source,
    int PreviousCheckQTY,
    int NewCheckQTY,
    int PreviousHistoricCheckQTY,
    int NewHistoricCheckQTY,
    DateTimeOffset ObservedLastCheck,
    bool GoogleWalletAttempted,
    bool GoogleWalletSucceeded,
    bool AppleWalletAttempted,
    bool AppleWalletSucceeded,
    string? ErrorSummary);

public sealed record RewardRedemptionRecord(
    long Id,
    Guid CardId,
    Guid BusinessId,
    Guid UserId,
    Guid? ActorBusinessId,
    int StampGoal,
    int RedeemedCheckQTY,
    int HistoricCheckQTY,
    string RewardText,
    bool GoogleWalletAttempted,
    bool GoogleWalletSucceeded,
    bool AppleWalletAttempted,
    bool AppleWalletSucceeded,
    string? ErrorSummary,
    DateTimeOffset RedeemedAt,
    DateTimeOffset CreatedAt);

public sealed record RewardRedemptionDto(
    Guid CardId,
    int StampGoal,
    int RedeemedCheckQTY,
    int HistoricCheckQTY,
    string RewardText,
    bool GoogleWalletAttempted,
    bool GoogleWalletSucceeded,
    bool AppleWalletAttempted,
    bool AppleWalletSucceeded,
    string? ErrorSummary,
    DateTimeOffset RedeemedAt);
