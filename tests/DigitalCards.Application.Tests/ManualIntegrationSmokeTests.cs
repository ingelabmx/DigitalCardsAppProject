using DigitalCards.Application;
using DigitalCards.Application.Abstractions;
using DigitalCards.Application.Models;
using DigitalCards.Application.Services;
using DigitalCards.Infrastructure;
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

        await app.RegisterClientAsync(new RegisterClientCommand(userName, "Full", "Smoke", clientEmail));
        var enrollment = await app.EnrollClientAsync(new EnrollClientCommand(
            business.Id,
            userName,
            GetRequired(configuration, "DigitalCards:PublicBaseUrl")));

        var google = await app.SelectGoogleWalletAsync(enrollment.Card.EnrollmentToken);
        Assert.NotNull(google);
        Assert.StartsWith("https://pay.google.com/gp/v/save/", google!.SaveUrl);

        var stamped = await app.AddStampAsync(new AddStampCommand(business.Id, userName));
        Assert.Equal(2, stamped.CurrentStamps);
        Assert.NotNull(stamped.GoogleObjectId);
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
}
