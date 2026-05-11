using DigitalCards.Infrastructure.Email;
using DigitalCards.Infrastructure.Wallets;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using MySqlConnector;

namespace DigitalCards.Infrastructure;

internal static class DigitalCardsIntegrationConfigurationValidator
{
    public static IntegrationProviders Validate(
        IConfiguration configuration,
        DigitalCardsInfrastructureOptions options,
        GoogleWalletOptions googleWalletOptions,
        AppleWalletOptions appleWalletOptions,
        SmtpEmailOptions emailOptions)
    {
        var persistenceProvider = ResolveProvider(options.PersistenceProvider, "InMemory");
        ValidateKnownProvider(
            persistenceProvider,
            "DigitalCards:PersistenceProvider",
            "InMemory",
            "MySql");

        string? digitalCardsConnectionString = null;
        if (IsProvider(persistenceProvider, "MySql"))
        {
            digitalCardsConnectionString = configuration.GetConnectionString("DigitalCards");
            ValidateMySqlConnectionString(digitalCardsConnectionString);
        }

        var emailProvider = ResolveProvider(emailOptions.Provider, "Fake");
        ValidateKnownProvider(
            emailProvider,
            "DigitalCards:Email:Provider",
            "Fake",
            "Smtp");

        if (IsProvider(emailProvider, "Smtp"))
        {
            ValidateSmtp(emailOptions);
            ValidateAbsoluteHttpUrl(
                options.PublicBaseUrl,
                "DigitalCards:PublicBaseUrl",
                "when SMTP email is enabled");
        }

        var googleWalletProvider = ResolveProvider(
            googleWalletOptions.Provider,
            options.UseFakeIntegrations ? "Fake" : "Google");
        ValidateKnownProvider(
            googleWalletProvider,
            "DigitalCards:GoogleWallet:Provider",
            "Fake",
            "Google");

        if (IsProvider(googleWalletProvider, "Google"))
        {
            ValidateGoogleWallet(googleWalletOptions);
        }

        var appleWalletProvider = ResolveProvider(appleWalletOptions.Provider, "Fake");
        ValidateKnownProvider(
            appleWalletProvider,
            "DigitalCards:AppleWallet:Provider",
            "Fake",
            "Apple");

        if (IsProvider(appleWalletProvider, "Apple"))
        {
            throw new InvalidOperationException(
                "DigitalCards:AppleWallet:Provider=Apple is reserved for the production Apple Wallet adapter, which is not implemented yet. Use Fake for now.");
        }

        return new IntegrationProviders(
            persistenceProvider,
            emailProvider,
            googleWalletProvider,
            appleWalletProvider,
            digitalCardsConnectionString);
    }

    private static void ValidateMySqlConnectionString(string? connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "ConnectionStrings:DigitalCards is required when DigitalCards:PersistenceProvider is MySql.");
        }

        MySqlConnectionStringBuilder builder;
        try
        {
            builder = new MySqlConnectionStringBuilder(connectionString);
        }
        catch (Exception exception) when (exception is ArgumentException or FormatException)
        {
            throw new InvalidOperationException("ConnectionStrings:DigitalCards is not a valid MySQL connection string.", exception);
        }

        Require(builder.Server, "ConnectionStrings:DigitalCards Server");
        Require(builder.Database, "ConnectionStrings:DigitalCards Database");
        Require(builder.UserID, "ConnectionStrings:DigitalCards User ID");
        Require(builder.Password, "ConnectionStrings:DigitalCards Password");
    }

    private static void ValidateGoogleWallet(GoogleWalletOptions options)
    {
        Require(options.IssuerId, "DigitalCards:GoogleWallet:IssuerId");
        Require(options.CredentialsFilePath, "DigitalCards:GoogleWallet:CredentialsFilePath");

        if (options.Origins.Length == 0)
        {
            throw new InvalidOperationException(
                "DigitalCards:GoogleWallet:Origins must contain at least one absolute HTTP(S) origin when real Google Wallet is enabled.");
        }

        for (var index = 0; index < options.Origins.Length; index++)
        {
            ValidateAbsoluteHttpUrl(
                options.Origins[index],
                $"DigitalCards:GoogleWallet:Origins:{index}",
                "when real Google Wallet is enabled");
        }
    }

    private static void ValidateSmtp(SmtpEmailOptions options)
    {
        Require(options.Host, "DigitalCards:Email:Host");
        Require(options.FromAddress, "DigitalCards:Email:FromAddress");
        Require(options.UserName, "DigitalCards:Email:UserName");
        Require(options.Password, "DigitalCards:Email:Password");

        if (options.Port <= 0)
        {
            throw new InvalidOperationException("DigitalCards:Email:Port must be greater than zero.");
        }

        if (!Enum.TryParse<SecureSocketOptions>(options.SecureSocket, ignoreCase: true, out _))
        {
            throw new InvalidOperationException("DigitalCards:Email:SecureSocket must be a valid MailKit SecureSocketOptions value.");
        }
    }

    private static void ValidateAbsoluteHttpUrl(string? value, string key, string reason)
    {
        if (string.IsNullOrWhiteSpace(value) ||
            !Uri.TryCreate(value.Trim(), UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            throw new InvalidOperationException($"{key} must be an absolute HTTP(S) URL {reason}.");
        }
    }

    private static void ValidateKnownProvider(
        string provider,
        string key,
        params string[] allowedValues)
    {
        if (allowedValues.Any(allowed => IsProvider(provider, allowed)))
        {
            return;
        }

        throw new InvalidOperationException($"{key} must be {string.Join(" or ", allowedValues)}.");
    }

    private static void Require(string? value, string key)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"{key} is required for the selected real integration.");
        }
    }

    private static string ResolveProvider(string? configuredProvider, string defaultProvider)
    {
        return string.IsNullOrWhiteSpace(configuredProvider)
            ? defaultProvider
            : configuredProvider.Trim();
    }

    private static bool IsProvider(string provider, string expected)
    {
        return string.Equals(provider, expected, StringComparison.OrdinalIgnoreCase);
    }

    public sealed record IntegrationProviders(
        string PersistenceProvider,
        string EmailProvider,
        string GoogleWalletProvider,
        string AppleWalletProvider,
        string? DigitalCardsConnectionString);
}
