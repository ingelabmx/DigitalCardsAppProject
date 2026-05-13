namespace DigitalCards.Application.Models;

public enum StampLedgerSource
{
    ModernBusiness,
    LegacySync,
    AdminRetry
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
