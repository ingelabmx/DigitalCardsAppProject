using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace DigitalCards.Web.Operations;

public sealed class DigitalCardsConfigurationHealthCheck : IHealthCheck
{
    private readonly IConfiguration _configuration;
    private readonly ProductionOperationsOptions _operations;

    public DigitalCardsConfigurationHealthCheck(
        IConfiguration configuration,
        IOptions<ProductionOperationsOptions> operations)
    {
        _configuration = configuration;
        _operations = operations.Value;
    }

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var failures = new List<string>();
        var digitalCards = _configuration.GetSection("DigitalCards");
        var publicBaseUrl = digitalCards["PublicBaseUrl"];
        var persistenceProvider = Provider(digitalCards["PersistenceProvider"], "InMemory");
        var emailProvider = Provider(digitalCards.GetSection("Email")["Provider"], "Fake");
        var googleProvider = Provider(digitalCards.GetSection("GoogleWallet")["Provider"], "Fake");
        var appleProvider = Provider(digitalCards.GetSection("AppleWallet")["Provider"], "Fake");

        if (Is(persistenceProvider, "MySql"))
        {
            Require(_configuration.GetConnectionString("DigitalCards"), "MySQL connection string", failures);
        }

        if (Is(emailProvider, "Smtp"))
        {
            var email = digitalCards.GetSection("Email");
            Require(email["Host"], "SMTP host", failures);
            Require(email["FromAddress"], "SMTP from address", failures);
            Require(email["UserName"], "SMTP username", failures);
            Require(email["Password"], "SMTP password", failures);
            RequireAbsoluteHttpUrl(publicBaseUrl, "public base URL for SMTP links", failures);
        }

        if (Is(googleProvider, "Google"))
        {
            var google = digitalCards.GetSection("GoogleWallet");
            Require(google["IssuerId"], "Google Wallet issuer id", failures);
            RequireExistingFile(google["CredentialsFilePath"], "Google Wallet credentials file", failures);

            var origins = google.GetSection("Origins").Get<string[]>() ?? [];
            if (origins.Length == 0)
            {
                failures.Add("Google Wallet origins are required.");
            }
            else
            {
                foreach (var origin in origins)
                {
                    RequireAbsoluteHttpUrl(origin, "Google Wallet origin", failures);
                }
            }
        }

        if (Is(appleProvider, "Apple"))
        {
            var apple = digitalCards.GetSection("AppleWallet");
            RequireAbsoluteHttpUrl(publicBaseUrl, "public base URL for Apple Wallet", failures);
            Require(apple["TeamIdentifier"], "Apple team identifier", failures);
            Require(apple["PassTypeIdentifier"], "Apple pass type identifier", failures);
            Require(apple["OrganizationName"], "Apple organization name", failures);
            RequireExistingFile(apple["CertificatePath"], "Apple pass certificate", failures);
            Require(apple["CertificatePassword"], "Apple certificate password", failures);
            RequireExistingFile(apple["WwdrCertificatePath"], "Apple WWDR certificate", failures);
            RequireExistingDirectory(apple["AssetsPath"], "Apple Wallet assets directory", failures);
            RequireMinimumLength(apple["AuthenticationTokenSecret"], 32, "Apple authentication token secret", failures);
            RequireAbsoluteHttpsUrl(apple["ApnsBaseUrl"], "Apple APNs base URL", failures);
        }

        if (_operations.RequireDataProtectionKeysForReadiness)
        {
            RequireExistingDirectory(
                _operations.DataProtectionKeysPath,
                "Data Protection keys directory",
                failures);
        }

        var data = new Dictionary<string, object>
        {
            ["persistenceProvider"] = persistenceProvider,
            ["emailProvider"] = emailProvider,
            ["googleWalletProvider"] = googleProvider,
            ["appleWalletProvider"] = appleProvider,
            ["publicHost"] = SafeHost(publicBaseUrl) ?? string.Empty,
            ["dataProtectionKeysConfigured"] = !string.IsNullOrWhiteSpace(_operations.DataProtectionKeysPath)
        };

        if (failures.Count > 0)
        {
            data["failureCount"] = failures.Count;
            return Task.FromResult(HealthCheckResult.Unhealthy(
                "Critical configuration is incomplete or invalid.",
                data: data));
        }

        return Task.FromResult(HealthCheckResult.Healthy("Critical configuration is valid.", data));
    }

    private static string Provider(string? value, string fallback)
    {
        return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
    }

    private static bool Is(string value, string expected)
    {
        return string.Equals(value, expected, StringComparison.OrdinalIgnoreCase);
    }

    private static void Require(string? value, string label, ICollection<string> failures)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            failures.Add($"{label} is required.");
        }
    }

    private static void RequireMinimumLength(
        string? value,
        int minimumLength,
        string label,
        ICollection<string> failures)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length < minimumLength)
        {
            failures.Add($"{label} is required.");
        }
    }

    private static void RequireExistingFile(string? path, string label, ICollection<string> failures)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            failures.Add($"{label} is required.");
        }
    }

    private static void RequireExistingDirectory(string? path, string label, ICollection<string> failures)
    {
        if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path))
        {
            failures.Add($"{label} is required.");
        }
    }

    private static void RequireAbsoluteHttpUrl(string? value, string label, ICollection<string> failures)
    {
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            failures.Add($"{label} must be an absolute HTTP(S) URL.");
        }
    }

    private static void RequireAbsoluteHttpsUrl(string? value, string label, ICollection<string> failures)
    {
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) ||
            uri.Scheme != Uri.UriSchemeHttps)
        {
            failures.Add($"{label} must be an absolute HTTPS URL.");
        }
    }

    private static string? SafeHost(string? value)
    {
        return Uri.TryCreate(value, UriKind.Absolute, out var uri) ? uri.Host : null;
    }
}
