namespace DigitalCards.Application.Models;

public sealed record AppleWalletPassRecord(
    string PassTypeIdentifier,
    string SerialNumber,
    Guid CardId,
    string AuthenticationTokenHash,
    string UpdateTag,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record AppleWalletDeviceRecord(
    string DeviceLibraryIdentifier,
    string PushToken,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public enum AppleWalletRegistrationStatus
{
    Created,
    AlreadyRegistered,
    Unauthorized,
    NotFound
}

public enum AppleWalletUnregistrationStatus
{
    Removed,
    Unauthorized,
    NotFound
}

public enum AppleWalletPassRequestStatus
{
    Ready,
    Unauthorized,
    NotFound
}

public sealed record AppleWalletPassRequestResult(
    AppleWalletPassRequestStatus Status,
    AppleWalletPassFile? PassFile);

public sealed record AppleWalletUpdatedPasses(
    IReadOnlyList<string> SerialNumbers,
    string LastUpdated);

public sealed record AppleWalletPushResult(
    bool Accepted,
    bool ShouldDeleteDevice,
    string Status);
