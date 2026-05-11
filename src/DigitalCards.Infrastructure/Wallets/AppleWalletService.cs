using System.Security.Cryptography;
using System.Text;
using DigitalCards.Application.Abstractions;
using DigitalCards.Application.Models;
using DigitalCards.Domain;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DigitalCards.Infrastructure.Wallets;

public sealed class AppleWalletService : IAppleWalletService
{
    private const string AuthorizationScheme = "ApplePass";

    private readonly IAppleWalletPassRepository _applePasses;
    private readonly AppleWalletPassPackageBuilder _builder;
    private readonly IBusinessRepository _businesses;
    private readonly IClientRepository _clients;
    private readonly IClock _clock;
    private readonly DigitalCardsInfrastructureOptions _infrastructureOptions;
    private readonly ILoyaltyCardRepository _loyaltyCards;
    private readonly ILogger<AppleWalletService> _logger;
    private readonly AppleWalletOptions _options;
    private readonly IAppleWalletPushSender _pushSender;

    public AppleWalletService(
        IOptions<AppleWalletOptions> options,
        IOptions<DigitalCardsInfrastructureOptions> infrastructureOptions,
        AppleWalletPassPackageBuilder builder,
        IAppleWalletPassRepository applePasses,
        IAppleWalletPushSender pushSender,
        IClientRepository clients,
        IBusinessRepository businesses,
        ILoyaltyCardRepository loyaltyCards,
        IClock clock,
        ILogger<AppleWalletService> logger)
    {
        _options = options.Value;
        _infrastructureOptions = infrastructureOptions.Value;
        _builder = builder;
        _applePasses = applePasses;
        _pushSender = pushSender;
        _clients = clients;
        _businesses = businesses;
        _loyaltyCards = loyaltyCards;
        _clock = clock;
        _logger = logger;
    }

    public Task<AppleWalletIssueResult> IssueAsync(
        LoyaltyCard card,
        Client client,
        Business business,
        CancellationToken cancellationToken = default)
    {
        var serialNumber = card.Id.ToString("N");
        _logger.LogInformation(
            "Prepared Apple Wallet pass download for serial {SerialNumber}.",
            serialNumber);

        return Task.FromResult(new AppleWalletIssueResult(
            AppleWalletIssueStatus.Ready,
            "Apple Wallet esta lista para descargarse.",
            DownloadUrl: null,
            SerialNumber: serialNumber));
    }

    public async Task<AppleWalletPassFile> CreatePassAsync(
        LoyaltyCard card,
        Client client,
        Business business,
        CancellationToken cancellationToken = default)
    {
        return await CreatePassFileAsync(card, client, business, cancellationToken);
    }

    public async Task<AppleWalletPassRequestResult> CreateUpdatedPassAsync(
        string passTypeIdentifier,
        string serialNumber,
        string? authorizationHeader,
        CancellationToken cancellationToken = default)
    {
        var pass = await _applePasses.FindPassAsync(passTypeIdentifier, serialNumber, cancellationToken);
        if (pass is null)
        {
            return new AppleWalletPassRequestResult(AppleWalletPassRequestStatus.NotFound, PassFile: null);
        }

        if (!IsAuthorized(pass, authorizationHeader))
        {
            return new AppleWalletPassRequestResult(AppleWalletPassRequestStatus.Unauthorized, PassFile: null);
        }

        var context = await FindCardContextBySerialAsync(serialNumber, cancellationToken);
        if (context is null)
        {
            return new AppleWalletPassRequestResult(AppleWalletPassRequestStatus.NotFound, PassFile: null);
        }

        var (card, client, business) = context.Value;
        var passFile = await CreatePassFileAsync(card, client, business, cancellationToken);
        return new AppleWalletPassRequestResult(AppleWalletPassRequestStatus.Ready, passFile);
    }

    public async Task<AppleWalletRegistrationStatus> RegisterDeviceAsync(
        string deviceLibraryIdentifier,
        string passTypeIdentifier,
        string serialNumber,
        string pushToken,
        string? authorizationHeader,
        CancellationToken cancellationToken = default)
    {
        var pass = await _applePasses.FindPassAsync(passTypeIdentifier, serialNumber, cancellationToken);
        if (pass is null)
        {
            return AppleWalletRegistrationStatus.NotFound;
        }

        if (!IsAuthorized(pass, authorizationHeader))
        {
            return AppleWalletRegistrationStatus.Unauthorized;
        }

        var now = _clock.UtcNow;
        await _applePasses.UpsertDeviceAsync(
            new AppleWalletDeviceRecord(deviceLibraryIdentifier, pushToken, now, now),
            cancellationToken);

        var created = await _applePasses.AddRegistrationAsync(
            deviceLibraryIdentifier,
            passTypeIdentifier,
            serialNumber,
            now,
            cancellationToken);

        _logger.LogInformation(
            "Apple Wallet device registration {RegistrationStatus} for pass type {PassTypeIdentifier} serial {SerialNumber}.",
            created ? "created" : "already-existed",
            passTypeIdentifier,
            serialNumber);

        return created
            ? AppleWalletRegistrationStatus.Created
            : AppleWalletRegistrationStatus.AlreadyRegistered;
    }

    public async Task<AppleWalletUnregistrationStatus> UnregisterDeviceAsync(
        string deviceLibraryIdentifier,
        string passTypeIdentifier,
        string serialNumber,
        string? authorizationHeader,
        CancellationToken cancellationToken = default)
    {
        var pass = await _applePasses.FindPassAsync(passTypeIdentifier, serialNumber, cancellationToken);
        if (pass is null)
        {
            return AppleWalletUnregistrationStatus.NotFound;
        }

        if (!IsAuthorized(pass, authorizationHeader))
        {
            return AppleWalletUnregistrationStatus.Unauthorized;
        }

        var removed = await _applePasses.RemoveRegistrationAsync(
            deviceLibraryIdentifier,
            passTypeIdentifier,
            serialNumber,
            cancellationToken);

        await _applePasses.DeleteDeviceIfOrphanedAsync(deviceLibraryIdentifier, cancellationToken);

        return removed
            ? AppleWalletUnregistrationStatus.Removed
            : AppleWalletUnregistrationStatus.NotFound;
    }

    public async Task<AppleWalletUpdatedPasses?> ListUpdatedPassesAsync(
        string deviceLibraryIdentifier,
        string passTypeIdentifier,
        string? previousLastUpdated,
        CancellationToken cancellationToken = default)
    {
        var passes = await _applePasses.ListUpdatedPassesForDeviceAsync(
            deviceLibraryIdentifier,
            passTypeIdentifier,
            previousLastUpdated,
            cancellationToken);

        if (passes.Count == 0)
        {
            return null;
        }

        var lastUpdated = passes.Max(pass => long.Parse(pass.UpdateTag)).ToString();
        return new AppleWalletUpdatedPasses(
            passes.Select(pass => pass.SerialNumber).ToArray(),
            lastUpdated);
    }

    public async Task NotifyPassUpdatedAsync(
        LoyaltyCard card,
        Client client,
        Business business,
        CancellationToken cancellationToken = default)
    {
        var pass = await _applePasses.FindPassByCardIdAsync(card.Id, cancellationToken);
        if (pass is null)
        {
            return;
        }

        var updateTag = CreateUpdateTag(_clock.UtcNow);
        await _applePasses.UpdatePassTagAsync(
            pass.PassTypeIdentifier,
            pass.SerialNumber,
            updateTag,
            _clock.UtcNow,
            cancellationToken);

        var devices = await _applePasses.ListDevicesForPassAsync(
            pass.PassTypeIdentifier,
            pass.SerialNumber,
            cancellationToken);

        foreach (var device in devices)
        {
            try
            {
                var result = await _pushSender.SendUpdateAsync(device.PushToken, pass.PassTypeIdentifier, cancellationToken);
                if (result.ShouldDeleteDevice)
                {
                    await _applePasses.RemoveRegistrationAsync(
                        device.DeviceLibraryIdentifier,
                        pass.PassTypeIdentifier,
                        pass.SerialNumber,
                        cancellationToken);
                    await _applePasses.DeleteDeviceIfOrphanedAsync(device.DeviceLibraryIdentifier, cancellationToken);
                }
            }
            catch (Exception exception) when (exception is HttpRequestException or IOException or InvalidOperationException)
            {
                _logger.LogWarning(
                    exception,
                    "Apple Wallet update push could not be sent for pass type {PassTypeIdentifier} serial {SerialNumber}.",
                    pass.PassTypeIdentifier,
                    pass.SerialNumber);
            }
        }
    }

    private async Task<AppleWalletPassFile> CreatePassFileAsync(
        LoyaltyCard card,
        Client client,
        Business business,
        CancellationToken cancellationToken)
    {
        var serialNumber = card.Id.ToString("N");
        var passTypeIdentifier = Require(_options.PassTypeIdentifier, "DigitalCards:AppleWallet:PassTypeIdentifier");
        var authToken = CreateAuthenticationToken(passTypeIdentifier, serialNumber);
        var now = _clock.UtcNow;
        var existing = await _applePasses.FindPassAsync(passTypeIdentifier, serialNumber, cancellationToken);
        var updateTag = existing?.UpdateTag ?? CreateUpdateTag(now);

        await _applePasses.UpsertPassAsync(
            new AppleWalletPassRecord(
                passTypeIdentifier,
                serialNumber,
                card.Id,
                HashAuthenticationToken(authToken),
                updateTag,
                existing?.CreatedAt ?? now,
                now),
            cancellationToken);

        var passFile = _builder.Build(
            card,
            client,
            business,
            _options,
            new AppleWalletPassPackageBuilder.AppleWalletPassBuildSettings(
                BuildWebServiceUrl(),
                authToken));

        _logger.LogInformation(
            "Generated Apple Wallet pass package for serial {SerialNumber}.",
            passFile.SerialNumber);

        return passFile;
    }

    private async Task<(LoyaltyCard Card, Client Client, Business Business)?> FindCardContextBySerialAsync(
        string serialNumber,
        CancellationToken cancellationToken)
    {
        var card = await _loyaltyCards.FindByEnrollmentTokenAsync(serialNumber, cancellationToken);
        if (card is null)
        {
            return null;
        }

        var client = await _clients.FindByIdAsync(card.ClientId, cancellationToken);
        var business = await _businesses.FindByIdAsync(card.BusinessId, cancellationToken);
        return client is null || business is null ? null : (card, client, business);
    }

    private string BuildWebServiceUrl()
    {
        var publicBaseUrl = Require(_infrastructureOptions.PublicBaseUrl, "DigitalCards:PublicBaseUrl");
        return $"{publicBaseUrl.TrimEnd('/')}/apple-wallet/";
    }

    private string CreateAuthenticationToken(string passTypeIdentifier, string serialNumber)
    {
        var secret = Require(_options.AuthenticationTokenSecret, "DigitalCards:AppleWallet:AuthenticationTokenSecret");
        var data = Encoding.UTF8.GetBytes($"{passTypeIdentifier}:{serialNumber}");
        return Convert.ToHexString(HMACSHA256.HashData(Encoding.UTF8.GetBytes(secret), data)).ToLowerInvariant();
    }

    private static string HashAuthenticationToken(string token)
    {
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token))).ToLowerInvariant();
    }

    private static string CreateUpdateTag(DateTimeOffset timestamp)
    {
        return timestamp.ToUnixTimeMilliseconds().ToString();
    }

    private static bool IsAuthorized(AppleWalletPassRecord pass, string? authorizationHeader)
    {
        if (string.IsNullOrWhiteSpace(authorizationHeader) ||
            !authorizationHeader.StartsWith($"{AuthorizationScheme} ", StringComparison.Ordinal))
        {
            return false;
        }

        var token = authorizationHeader[AuthorizationScheme.Length..].Trim();
        var providedHash = HashAuthenticationToken(token);

        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(providedHash),
            Encoding.UTF8.GetBytes(pass.AuthenticationTokenHash));
    }

    private static string Require(string? value, string key)
    {
        return string.IsNullOrWhiteSpace(value)
            ? throw new InvalidOperationException($"{key} is required when real Apple Wallet is enabled.")
            : value;
    }
}
