using System.Net;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using DigitalCards.Application.Abstractions;
using DigitalCards.Application.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DigitalCards.Infrastructure.Wallets;

public sealed class AppleWalletPushSender : IAppleWalletPushSender
{
    private readonly ILogger<AppleWalletPushSender> _logger;
    private readonly AppleWalletOptions _options;

    public AppleWalletPushSender(
        IOptions<AppleWalletOptions> options,
        ILogger<AppleWalletPushSender> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task<AppleWalletPushResult> SendUpdateAsync(
        string pushToken,
        string passTypeIdentifier,
        CancellationToken cancellationToken = default)
    {
        var certificatePath = Require(_options.CertificatePath, "DigitalCards:AppleWallet:CertificatePath");
        var certificatePassword = Require(_options.CertificatePassword, "DigitalCards:AppleWallet:CertificatePassword");

        var baseUri = CreateApnsBaseUri();
        var certificate = new X509Certificate2(
            certificatePath,
            certificatePassword,
            X509KeyStorageFlags.UserKeySet | X509KeyStorageFlags.PersistKeySet);

        using var handler = new SocketsHttpHandler
        {
            SslOptions = new SslClientAuthenticationOptions
            {
                EnabledSslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13,
                ClientCertificates = new X509CertificateCollection { certificate }
            }
        };

        using var client = new HttpClient(handler)
        {
            BaseAddress = baseUri
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, $"/3/device/{pushToken}")
        {
            Version = HttpVersion.Version20,
            VersionPolicy = HttpVersionPolicy.RequestVersionExact,
            Content = new StringContent("{}", Encoding.UTF8, "application/json")
        };
        request.Headers.TryAddWithoutValidation("apns-topic", passTypeIdentifier);

        using var response = await client.SendAsync(request, cancellationToken);
        var status = $"{(int)response.StatusCode}";
        if (response.IsSuccessStatusCode)
        {
            _logger.LogInformation("Apple Wallet update push accepted for pass type {PassTypeIdentifier}.", passTypeIdentifier);
            return new AppleWalletPushResult(Accepted: true, ShouldDeleteDevice: false, Status: status);
        }

        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        var shouldDelete = (response.StatusCode is HttpStatusCode.Gone or HttpStatusCode.BadRequest) &&
            (body.Contains("BadDeviceToken", StringComparison.OrdinalIgnoreCase) ||
             body.Contains("Unregistered", StringComparison.OrdinalIgnoreCase));

        _logger.LogWarning(
            "Apple Wallet update push failed for pass type {PassTypeIdentifier} with APNs status {StatusCode}.",
            passTypeIdentifier,
            (int)response.StatusCode);

        return new AppleWalletPushResult(Accepted: false, ShouldDeleteDevice: shouldDelete, Status: status);
    }

    private Uri CreateApnsBaseUri()
    {
        if (string.IsNullOrWhiteSpace(_options.ApnsBaseUrl) ||
            !Uri.TryCreate(_options.ApnsBaseUrl.TrimEnd('/'), UriKind.Absolute, out var uri) ||
            uri.Scheme != Uri.UriSchemeHttps)
        {
            throw new InvalidOperationException("DigitalCards:AppleWallet:ApnsBaseUrl must be an absolute HTTPS URL.");
        }

        return uri;
    }

    private static string Require(string? value, string key)
    {
        return string.IsNullOrWhiteSpace(value)
            ? throw new InvalidOperationException($"{key} is required when Apple Wallet APNs is enabled.")
            : value;
    }
}
