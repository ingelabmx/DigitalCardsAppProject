using System.IO.Compression;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using DigitalCards.Application.Abstractions;
using DigitalCards.Application.Models;
using DigitalCards.Domain;
using DigitalCards.Infrastructure;
using DigitalCards.Infrastructure.Wallets;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DigitalCards.Application.Tests;

public sealed class AppleWalletServiceTests
{
    [Fact]
    public async Task RegisterListAndUnregisterDevice_UsesStoredPassAuthorization()
    {
        using var provider = BuildAppleProvider();
        var appleWallet = provider.GetRequiredService<IAppleWalletService>();
        var repository = provider.GetRequiredService<IAppleWalletPassRepository>();
        var passTypeIdentifier = "pass.com.example.digitalcards";
        var serialNumber = "serial-123";
        var token = "apple-auth-token";

        await repository.UpsertPassAsync(new AppleWalletPassRecord(
            passTypeIdentifier,
            serialNumber,
            Guid.NewGuid(),
            HashToken(token),
            "1000",
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow));

        var registered = await appleWallet.RegisterDeviceAsync(
            "device-library-id",
            passTypeIdentifier,
            serialNumber,
            "push-token",
            $"ApplePass {token}");

        var updated = await appleWallet.ListUpdatedPassesAsync(
            "device-library-id",
            passTypeIdentifier,
            previousLastUpdated: null);

        var removed = await appleWallet.UnregisterDeviceAsync(
            "device-library-id",
            passTypeIdentifier,
            serialNumber,
            $"ApplePass {token}");

        var noUpdates = await appleWallet.ListUpdatedPassesAsync(
            "device-library-id",
            passTypeIdentifier,
            previousLastUpdated: null);

        Assert.Equal(AppleWalletRegistrationStatus.Created, registered);
        Assert.NotNull(updated);
        Assert.Equal("1000", updated!.LastUpdated);
        Assert.Equal(new[] { serialNumber }, updated.SerialNumbers);
        Assert.Equal(AppleWalletUnregistrationStatus.Removed, removed);
        Assert.Null(noUpdates);
    }

    [Fact]
    public async Task RegisterDevice_RejectsMissingAuthorization()
    {
        using var provider = BuildAppleProvider();
        var appleWallet = provider.GetRequiredService<IAppleWalletService>();
        var repository = provider.GetRequiredService<IAppleWalletPassRepository>();
        var passTypeIdentifier = "pass.com.example.digitalcards";

        await repository.UpsertPassAsync(new AppleWalletPassRecord(
            passTypeIdentifier,
            "serial-123",
            Guid.NewGuid(),
            HashToken("apple-auth-token"),
            "1000",
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow));

        var registered = await appleWallet.RegisterDeviceAsync(
            "device-library-id",
            passTypeIdentifier,
            "serial-123",
            "push-token",
            authorizationHeader: null);

        Assert.Equal(AppleWalletRegistrationStatus.Unauthorized, registered);
    }

    [Fact]
    public async Task CreateUpdatedPass_ResolvesCurrentCardBySerialNumber()
    {
        using var files = CreateAppleWalletTestFiles();
        using var provider = BuildAppleProvider(new Dictionary<string, string?>
        {
            ["DigitalCards:AppleWallet:AssetsPath"] = files.AssetsPath,
            ["DigitalCards:AppleWallet:CertificatePath"] = files.CertificatePath,
            ["DigitalCards:AppleWallet:CertificatePassword"] = files.CertificatePassword,
            ["DigitalCards:AppleWallet:WwdrCertificatePath"] = files.WwdrCertificatePath
        });
        var appleWallet = provider.GetRequiredService<IAppleWalletService>();
        var businesses = provider.GetRequiredService<IBusinessRepository>();
        var clients = provider.GetRequiredService<IClientRepository>();
        var cards = provider.GetRequiredService<ILoyaltyCardRepository>();

        var business = new Business(
            Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
            "Balboa Water",
            "balboa@example.test",
            "hash",
            string.Empty,
            publicName: "Runni Cafe",
            programName: "Runni Rewards",
            stampGoal: 10);
        var client = new Client(
            Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"),
            "runni-client",
            "Maria Fernanda",
            "Lopez Perez",
            "maria@example.test");
        var card = LoyaltyCard.Restore(
            Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            client.Id,
            business.Id,
            enrollmentToken: "opaque-enrollment-token",
            currentStamps: 9,
            lifetimeStamps: 9,
            DateTimeOffset.Parse("2026-05-11T00:00:00Z"),
            DateTimeOffset.Parse("2026-05-11T00:09:00Z"),
            googleObjectId: null,
            googleSaveUrl: null);

        await businesses.AddAsync(business);
        await clients.AddAsync(client);
        await cards.AddAsync(card);

        var firstPass = await appleWallet.CreatePassAsync(card, client, business);
        var authToken = ReadAuthenticationToken(firstPass);

        card.AddStamp(DateTimeOffset.Parse("2026-05-11T00:10:00Z"), business.StampGoal);
        await cards.UpdateAsync(card);

        var updated = await appleWallet.CreateUpdatedPassAsync(
            "pass.com.example.digitalcards",
            card.Id.ToString("N"),
            $"ApplePass {authToken}");

        Assert.Equal(AppleWalletPassRequestStatus.Ready, updated.Status);
        Assert.NotNull(updated.PassFile);
        Assert.Equal("10 de 10", ReadStampProgress(updated.PassFile!));
    }

    [Fact]
    public async Task CreateUpdatedPass_UsesResetStampProgressAfterRewardRedemption()
    {
        using var files = CreateAppleWalletTestFiles();
        using var provider = BuildAppleProvider(new Dictionary<string, string?>
        {
            ["DigitalCards:AppleWallet:AssetsPath"] = files.AssetsPath,
            ["DigitalCards:AppleWallet:CertificatePath"] = files.CertificatePath,
            ["DigitalCards:AppleWallet:CertificatePassword"] = files.CertificatePassword,
            ["DigitalCards:AppleWallet:WwdrCertificatePath"] = files.WwdrCertificatePath
        });
        var appleWallet = provider.GetRequiredService<IAppleWalletService>();
        var businesses = provider.GetRequiredService<IBusinessRepository>();
        var clients = provider.GetRequiredService<IClientRepository>();
        var cards = provider.GetRequiredService<ILoyaltyCardRepository>();

        var business = new Business(
            Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd"),
            "Runni Cafe",
            "runni@example.test",
            "hash",
            string.Empty,
            stampGoal: 10);
        var client = new Client(
            Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee"),
            "reward-client",
            "Reward",
            "Client",
            "reward@example.test");
        var card = LoyaltyCard.Restore(
            Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffffffff"),
            client.Id,
            business.Id,
            enrollmentToken: "reward-token",
            currentStamps: 10,
            lifetimeStamps: 10,
            DateTimeOffset.Parse("2026-05-11T00:00:00Z"),
            DateTimeOffset.Parse("2026-05-11T00:10:00Z"),
            googleObjectId: null,
            googleSaveUrl: null);

        await businesses.AddAsync(business);
        await clients.AddAsync(client);
        await cards.AddAsync(card);

        var firstPass = await appleWallet.CreatePassAsync(card, client, business);
        var authToken = ReadAuthenticationToken(firstPass);

        card.RedeemReward(DateTimeOffset.Parse("2026-05-11T00:11:00Z"), business.StampGoal);
        await cards.UpdateAsync(card);

        var updated = await appleWallet.CreateUpdatedPassAsync(
            "pass.com.example.digitalcards",
            card.Id.ToString("N"),
            $"ApplePass {authToken}");

        Assert.Equal(AppleWalletPassRequestStatus.Ready, updated.Status);
        Assert.NotNull(updated.PassFile);
        Assert.Equal("0 de 10", ReadStampProgress(updated.PassFile!));
    }

    private static ServiceProvider BuildAppleProvider(IReadOnlyDictionary<string, string?>? overrides = null)
    {
        var settings = new Dictionary<string, string?>
        {
            ["DigitalCards:PublicBaseUrl"] = "https://example.test",
            ["DigitalCards:PersistenceProvider"] = "InMemory",
            ["DigitalCards:GoogleWallet:Provider"] = "Fake",
            ["DigitalCards:Email:Provider"] = "Fake",
            ["DigitalCards:AppleWallet:Provider"] = "Apple",
            ["DigitalCards:AppleWallet:TeamIdentifier"] = "TEAMID1234",
            ["DigitalCards:AppleWallet:PassTypeIdentifier"] = "pass.com.example.digitalcards",
            ["DigitalCards:AppleWallet:OrganizationName"] = "DigitalCards",
            ["DigitalCards:AppleWallet:CertificatePath"] = @"C:\secure\apple-pass-certificate.p12",
            ["DigitalCards:AppleWallet:CertificatePassword"] = "secret",
            ["DigitalCards:AppleWallet:WwdrCertificatePath"] = @"C:\secure\AppleWWDR.cer",
            ["DigitalCards:AppleWallet:AssetsPath"] = @"C:\secure\apple-assets",
            ["DigitalCards:AppleWallet:AuthenticationTokenSecret"] = "this-is-a-long-apple-wallet-test-secret",
            ["DigitalCards:AppleWallet:ApnsBaseUrl"] = "https://api.push.apple.com"
        };
        if (overrides is not null)
        {
            foreach (var pair in overrides)
            {
                settings[pair.Key] = pair.Value;
            }
        }

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(settings)
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDigitalCardsInfrastructure(configuration);
        return services.BuildServiceProvider();
    }

    private static string ReadAuthenticationToken(AppleWalletPassFile passFile)
    {
        using var document = ReadPassJson(passFile);
        return document.RootElement.GetProperty("authenticationToken").GetString()!;
    }

    private static string ReadStampProgress(AppleWalletPassFile passFile)
    {
        using var document = ReadPassJson(passFile);
        return document.RootElement
            .GetProperty("generic")
            .GetProperty("secondaryFields")[1]
            .GetProperty("value")
            .GetString()!;
    }

    private static JsonDocument ReadPassJson(AppleWalletPassFile passFile)
    {
        using var archive = new ZipArchive(new MemoryStream(passFile.Content), ZipArchiveMode.Read);
        var entry = archive.GetEntry("pass.json") ?? throw new InvalidOperationException("pass.json was not found.");
        using var stream = entry.Open();
        return JsonDocument.Parse(stream);
    }

    private static string HashToken(string token)
    {
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token))).ToLowerInvariant();
    }

    private static AppleWalletTestFiles CreateAppleWalletTestFiles()
    {
        var root = Path.Combine(Path.GetTempPath(), $"digitalcards-apple-{Guid.NewGuid():N}");
        var assetsPath = Path.Combine(root, "assets");
        var certificatePath = Path.Combine(root, "pass.p12");
        var wwdrCertificatePath = Path.Combine(root, "wwdr.cer");
        const string certificatePassword = "test-password";

        Directory.CreateDirectory(assetsPath);
        File.WriteAllBytes(Path.Combine(assetsPath, "icon.png"), TinyPng());
        WriteSigningCertificate(certificatePath, certificatePassword);
        WriteWwdrCertificate(wwdrCertificatePath);

        return new AppleWalletTestFiles(
            root,
            assetsPath,
            certificatePath,
            wwdrCertificatePath,
            certificatePassword);
    }

    private static void WriteSigningCertificate(string path, string password)
    {
        using var key = RSA.Create(2048);
        var request = new CertificateRequest(
            "CN=Pass Type ID pass.com.example.digitalcards,O=Example",
            key,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);
        using var certificate = request.CreateSelfSigned(
            DateTimeOffset.UtcNow.AddDays(-1),
            DateTimeOffset.UtcNow.AddDays(1));
        File.WriteAllBytes(path, certificate.Export(X509ContentType.Pkcs12, password));
    }

    private static void WriteWwdrCertificate(string path)
    {
        using var key = RSA.Create(2048);
        var request = new CertificateRequest(
            "CN=Apple Worldwide Developer Relations Certification Authority,O=Apple Inc.",
            key,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);
        using var certificate = request.CreateSelfSigned(
            DateTimeOffset.UtcNow.AddDays(-1),
            DateTimeOffset.UtcNow.AddDays(1));
        File.WriteAllBytes(path, certificate.Export(X509ContentType.Cert));
    }

    private static byte[] TinyPng()
    {
        return Convert.FromBase64String(
            "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+/p9sAAAAASUVORK5CYII=");
    }

    private sealed record AppleWalletTestFiles(
        string Root,
        string AssetsPath,
        string CertificatePath,
        string WwdrCertificatePath,
        string CertificatePassword) : IDisposable
    {
        public void Dispose()
        {
            if (Directory.Exists(Root))
            {
                Directory.Delete(Root, recursive: true);
            }
        }
    }
}
