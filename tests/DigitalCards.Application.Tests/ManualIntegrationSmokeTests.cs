using System.IO.Compression;
using System.Security.Cryptography;
using System.Security.Cryptography.Pkcs;
using System.Text.Json;
using DigitalCards.Application;
using DigitalCards.Application.Abstractions;
using DigitalCards.Application.Models;
using DigitalCards.Application.Services;
using DigitalCards.Infrastructure;
using DigitalCards.Infrastructure.Wallets;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DigitalCards.Application.Tests;

public sealed class ManualIntegrationSmokeTests
{
    [ManualSmokeFact("RUN_MYSQL_GOOGLE_SMOKE")]
    public async Task MySqlGoogleSmoke_CreatesGoogleWalletAndPatchesStamp_WithFakeEmail()
    {
        var configuration = BuildLocalConfiguration(new Dictionary<string, string?>
        {
            ["DigitalCards:PersistenceProvider"] = "MySql",
            ["DigitalCards:GoogleWallet:Provider"] = "Google",
            ["DigitalCards:Email:Provider"] = "Fake"
        });
        var provider = BuildProvider(configuration);
        var app = provider.GetRequiredService<DigitalCardsAppService>();
        var business = await LoginSmokeBusinessAsync(app, configuration);
        var userName = NewUserName("gwsql");
        var clientEmail = $"{userName}@{configuration["DigitalCards:Smoke:ClientEmailDomain"] ?? "example.test"}";

        await app.RegisterClientAsync(new RegisterClientCommand(userName, "Google", "Smoke", clientEmail));
        var enrollment = await app.EnrollClientAsync(new EnrollClientCommand(
            business.Id,
            userName,
            GetRequired(configuration, "DigitalCards:PublicBaseUrl")));

        var outbox = provider.GetRequiredService<IWalletEmailOutbox>();
        var messages = await outbox.ListAsync();
        Assert.Contains(messages, message => message.To == clientEmail);

        var google = await app.SelectGoogleWalletAsync(enrollment.Card.EnrollmentToken);
        Assert.NotNull(google);
        Assert.StartsWith("https://pay.google.com/gp/v/save/", google!.SaveUrl);

        var stamped = await app.AddStampAsync(new AddStampCommand(business.Id, userName));
        Assert.Equal(2, stamped.CurrentStamps);
        Assert.NotNull(stamped.GoogleObjectId);
    }

    [ManualSmokeFact("RUN_SMTP_SMOKE")]
    public async Task SmtpSmoke_SendsWalletEnrollmentEmailOnly()
    {
        var configuration = BuildLocalConfiguration(new Dictionary<string, string?>
        {
            ["DigitalCards:PersistenceProvider"] = "InMemory",
            ["DigitalCards:GoogleWallet:Provider"] = "Fake",
            ["DigitalCards:Email:Provider"] = "Smtp"
        });
        var provider = BuildProvider(configuration);
        var emailSender = provider.GetRequiredService<IEmailSender>();
        var recipient = GetRequired(configuration, "DigitalCards:Email:SmokeRecipient");

        await emailSender.SendWalletEnrollmentAsync(new WalletEnrollmentEmail(
            recipient,
            "Smoke Recipient",
            "DigitalCards Smoke",
            $"{GetRequired(configuration, "DigitalCards:PublicBaseUrl")}/Wallet/Select/smoke-token",
            DateTimeOffset.UtcNow));
    }

    [ManualSmokeFact("RUN_FULL_REAL_SMOKE")]
    public async Task FullRealSmoke_UsesMySqlGoogleWalletAndSmtp()
    {
        var configuration = BuildLocalConfiguration(new Dictionary<string, string?>
        {
            ["DigitalCards:PersistenceProvider"] = "MySql",
            ["DigitalCards:GoogleWallet:Provider"] = "Google",
            ["DigitalCards:Email:Provider"] = "Smtp"
        });
        var provider = BuildProvider(configuration);
        var app = provider.GetRequiredService<DigitalCardsAppService>();
        var business = await LoginSmokeBusinessAsync(app, configuration);
        var userName = NewUserName("full");
        var clientEmail = BuildUniqueRecipient(GetRequired(configuration, "DigitalCards:Email:SmokeRecipient"), userName);

        userName = await EnsureSmokeClientAsync(app, provider, userName, clientEmail);
        var enrollment = await app.EnrollClientAsync(new EnrollClientCommand(
            business.Id,
            userName,
            GetRequired(configuration, "DigitalCards:PublicBaseUrl")));
        var currentStampsBeforeAdd = enrollment.Card.CurrentStamps;
        var lifetimeStampsBeforeAdd = enrollment.Card.LifetimeStamps;

        var google = await app.SelectGoogleWalletAsync(enrollment.Card.EnrollmentToken);
        Assert.NotNull(google);
        Assert.StartsWith("https://pay.google.com/gp/v/save/", google!.SaveUrl);

        var stamped = await app.AddStampAsync(new AddStampCommand(business.Id, userName));
        Assert.Equal(currentStampsBeforeAdd >= 9 ? 0 : currentStampsBeforeAdd + 1, stamped.CurrentStamps);
        Assert.Equal(lifetimeStampsBeforeAdd + 1, stamped.LifetimeStamps);
        Assert.NotNull(stamped.GoogleObjectId);
    }

    [ManualSmokeFact("RUN_FULL_REAL_WALLET_SMOKE")]
    public async Task FullRealWalletSmoke_UsesMySqlGoogleSmtpAndAppleWallet()
    {
        var configuration = BuildLocalConfiguration(new Dictionary<string, string?>
        {
            ["DigitalCards:PersistenceProvider"] = "MySql",
            ["DigitalCards:GoogleWallet:Provider"] = "Google",
            ["DigitalCards:Email:Provider"] = "Smtp",
            ["DigitalCards:AppleWallet:Provider"] = "Apple"
        });
        using var provider = BuildProvider(configuration);
        var app = provider.GetRequiredService<DigitalCardsAppService>();
        var business = await LoginSmokeBusinessAsync(app, configuration);
        var userName = NewUserName("wallet");
        var clientEmail = BuildUniqueRecipient(GetRequired(configuration, "DigitalCards:Email:SmokeRecipient"), userName);

        userName = await EnsureSmokeClientAsync(app, provider, userName, clientEmail);
        var enrollment = await app.EnrollClientAsync(new EnrollClientCommand(
            business.Id,
            userName,
            GetRequired(configuration, "DigitalCards:PublicBaseUrl")));

        var google = await app.SelectGoogleWalletAsync(enrollment.Card.EnrollmentToken);
        var apple = await app.DownloadAppleWalletPassAsync(enrollment.Card.EnrollmentToken);
        var stamped = await app.AddStampAsync(new AddStampCommand(business.Id, userName));

        Assert.NotNull(google);
        Assert.StartsWith("https://pay.google.com/gp/v/save/", google!.SaveUrl);
        Assert.NotNull(apple);
        Assert.Equal(AppleWalletPassPackageBuilder.ContentType, apple!.ContentType);
        Assert.EndsWith(".pkpass", apple.FileName, StringComparison.OrdinalIgnoreCase);
        Assert.NotNull(stamped.GoogleObjectId);
    }

    [ManualSmokeFact("RUN_APPLE_PKPASS_SMOKE")]
    public async Task ApplePkpassSmoke_GeneratesSignedPackage_WithLocalCertificates()
    {
        var configuration = BuildLocalConfiguration(new Dictionary<string, string?>
        {
            ["DigitalCards:PersistenceProvider"] = "InMemory",
            ["DigitalCards:GoogleWallet:Provider"] = "Fake",
            ["DigitalCards:Email:Provider"] = "Fake",
            ["DigitalCards:AppleWallet:Provider"] = "Apple"
        });
        using var provider = BuildProvider(configuration);
        var app = provider.GetRequiredService<DigitalCardsAppService>();
        var userName = NewUserName("apple");

        var client = await app.RegisterClientAsync(new RegisterClientCommand(
            userName,
            "Apple",
            "Smoke",
            $"{userName}@example.test"));

        var business = await app.LoginBusinessAsync(new BusinessLoginCommand(
            "demo@digitalcards.test",
            "business123"));
        Assert.NotNull(business);

        var enrollment = await app.EnrollClientAsync(new EnrollClientCommand(
            business!.Id,
            client.UserName,
            "https://example.test"));

        var passFile = await app.DownloadAppleWalletPassAsync(enrollment.Card.EnrollmentToken);

        Assert.NotNull(passFile);
        Assert.Equal(AppleWalletPassPackageBuilder.ContentType, passFile!.ContentType);
        Assert.EndsWith(".pkpass", passFile.FileName, StringComparison.OrdinalIgnoreCase);
        Assert.NotEmpty(passFile.Content);

        using var archive = new ZipArchive(new MemoryStream(passFile.Content), ZipArchiveMode.Read);
        Assert.NotNull(archive.GetEntry("pass.json"));
        Assert.NotNull(archive.GetEntry("manifest.json"));
        Assert.NotNull(archive.GetEntry("signature"));
        Assert.NotNull(archive.GetEntry("icon.png"));

        using var passJson = JsonDocument.Parse(ReadEntryText(archive, "pass.json"));
        var root = passJson.RootElement;
        Assert.Equal(GetRequired(configuration, "DigitalCards:AppleWallet:PassTypeIdentifier"), root.GetProperty("passTypeIdentifier").GetString());
        Assert.Equal(GetRequired(configuration, "DigitalCards:AppleWallet:TeamIdentifier"), root.GetProperty("teamIdentifier").GetString());
        Assert.Equal(GetRequired(configuration, "DigitalCards:AppleWallet:OrganizationName"), root.GetProperty("organizationName").GetString());
        Assert.True(root.TryGetProperty("storeCard", out _));

        using var manifestJson = JsonDocument.Parse(ReadEntryText(archive, "manifest.json"));
        var manifest = manifestJson.RootElement;
        Assert.True(manifest.TryGetProperty("pass.json", out _));
        Assert.True(manifest.TryGetProperty("icon.png", out _));
        Assert.False(manifest.TryGetProperty("manifest.json", out _));
        Assert.False(manifest.TryGetProperty("signature", out _));

        var signature = ReadEntryBytes(archive, "signature");
        var manifestBytes = ReadEntryBytes(archive, "manifest.json");
        var signedCms = new SignedCms(new ContentInfo(manifestBytes), detached: true);
        signedCms.Decode(signature);
        var signedAttributes = signedCms.SignerInfos[0].SignedAttributes;
        Assert.Contains(signedAttributes.Cast<CryptographicAttributeObject>(), attribute => attribute.Oid?.Value == "1.2.840.113549.1.9.5");
    }

    private static ServiceProvider BuildProvider(IConfiguration configuration)
    {
        var services = new ServiceCollection();
        services.AddSingleton(configuration);
        services.AddLogging();
        services.AddDigitalCardsApplication();
        services.AddDigitalCardsInfrastructure(configuration);
        return services.BuildServiceProvider();
    }

    private static IConfiguration BuildLocalConfiguration(Dictionary<string, string?> overrides)
    {
        var localConfigurationPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".digitalcards",
            "appsettings.Local.json");

        return new ConfigurationBuilder()
            .AddJsonFile(localConfigurationPath, optional: false)
            .AddEnvironmentVariables()
            .AddInMemoryCollection(overrides)
            .Build();
    }

    private static async Task<BusinessDto> LoginSmokeBusinessAsync(
        DigitalCardsAppService app,
        IConfiguration configuration)
    {
        var business = await app.LoginBusinessAsync(new BusinessLoginCommand(
            GetRequired(configuration, "DigitalCards:Smoke:BusinessEmail"),
            GetRequired(configuration, "DigitalCards:Smoke:BusinessPassword")));

        return business ?? throw new InvalidOperationException("Configured smoke business credentials were not accepted.");
    }

    private static async Task<string> EnsureSmokeClientAsync(
        DigitalCardsAppService app,
        IServiceProvider provider,
        string userName,
        string clientEmail)
    {
        try
        {
            var client = await app.RegisterClientAsync(new RegisterClientCommand(userName, "Full", "Smoke", clientEmail));
            return client.UserName;
        }
        catch (InvalidOperationException exception) when (
            exception.Message.Contains("already exists", StringComparison.OrdinalIgnoreCase))
        {
            var clients = provider.GetRequiredService<IClientRepository>();
            var existingClient = await clients.FindByUserNameOrEmailAsync(clientEmail);

            return existingClient?.UserName
                ?? throw new InvalidOperationException("The full smoke client email already exists but could not be loaded.");
        }
    }

    private static string GetRequired(IConfiguration configuration, string key)
    {
        var value = configuration[key];
        return string.IsNullOrWhiteSpace(value)
            ? throw new InvalidOperationException($"{key} is required for this manual smoke test.")
            : value;
    }

    private static string NewUserName(string prefix)
    {
        return $"{prefix}{Guid.NewGuid():N}"[..12];
    }

    private static string BuildUniqueRecipient(string recipient, string tag)
    {
        var at = recipient.IndexOf('@', StringComparison.Ordinal);
        if (at <= 0)
        {
            throw new InvalidOperationException("DigitalCards:Email:SmokeRecipient must be a valid email address.");
        }

        var localPart = recipient[..at];
        var domainPart = recipient[at..];
        var maxTagLength = 30 - localPart.Length - domainPart.Length - 1;

        if (maxTagLength > 0)
        {
            var normalizedTag = new string(tag.Where(char.IsLetterOrDigit).ToArray());
            var suffix = normalizedTag[..Math.Min(normalizedTag.Length, Math.Min(maxTagLength, 8))];
            return $"{localPart}+{suffix}{domainPart}";
        }

        if (recipient.Length <= 30)
        {
            return recipient;
        }

        throw new InvalidOperationException("DigitalCards:Email:SmokeRecipient must fit the legacy UserClient.UserEmail varchar(30) column.");
    }

    private static string ReadEntryText(ZipArchive archive, string entryName)
    {
        using var memory = new MemoryStream(ReadEntryBytes(archive, entryName));
        using var reader = new StreamReader(memory);
        return reader.ReadToEnd();
    }

    private static byte[] ReadEntryBytes(ZipArchive archive, string entryName)
    {
        var entry = archive.GetEntry(entryName)
            ?? throw new InvalidOperationException($"The generated Apple Wallet pass is missing {entryName}.");

        using var stream = entry.Open();
        using var memory = new MemoryStream();
        stream.CopyTo(memory);
        return memory.ToArray();
    }
}
