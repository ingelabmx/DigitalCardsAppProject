namespace DigitalCards.Application.Models;

public sealed record RegisterClientCommand(
    string UserName,
    string FirstName,
    string LastName,
    string Email,
    string Password = "");

public sealed record ClientLoginCommand(string UserNameOrEmail, string Password);

public sealed record BusinessLoginCommand(string Email, string Password);

public sealed record EnrollClientCommand(Guid BusinessId, string UserNameOrEmail, string BaseUrl);

public sealed record AddStampCommand(Guid BusinessId, string UserNameOrEmail);

public sealed record ClientDto(Guid Id, string UserName, string FirstName, string LastName, string Email);

public sealed record BusinessDto(Guid Id, string Name, string Email, string LogoPath);

public sealed record LoyaltyCardDto(
    Guid Id,
    string EnrollmentToken,
    string BusinessName,
    string ClientUserName,
    int CurrentStamps,
    int LifetimeStamps,
    string? GoogleObjectId,
    string? GoogleSaveUrl);

public sealed record ClientLoyaltyCardDto(
    Guid Id,
    string WalletSelectToken,
    string BusinessName,
    string ClientUserName,
    int CurrentStamps,
    int LifetimeStamps,
    bool GoogleIssued,
    string? GoogleSaveUrl);

public sealed record EnrollClientResult(LoyaltyCardDto Card, string EnrollmentUrl);

public sealed record BusinessCardDto(
    Guid Id,
    string EnrollmentToken,
    ClientDto Client,
    string BusinessName,
    int CurrentStamps,
    int LifetimeStamps,
    DateTimeOffset LastStampedAt,
    bool GoogleIssued,
    bool AppleTracked,
    int AppleRegisteredDeviceCount,
    DateTimeOffset? AppleUpdatedAt,
    IReadOnlyList<StampLedgerEventDto> RecentStampEvents);

public sealed record ResendWalletEmailResult(BusinessCardDto Card, string EnrollmentUrl);

public sealed record WalletLandingDto(
    string Token,
    string BusinessName,
    string ClientName,
    int CurrentStamps,
    int LifetimeStamps,
    bool HasGooglePass);

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
    DateTimeOffset CreatedAt);
