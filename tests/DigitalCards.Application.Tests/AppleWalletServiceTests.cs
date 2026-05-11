using System.Security.Cryptography;
using System.Text;
using DigitalCards.Application.Abstractions;
using DigitalCards.Application.Models;
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

    private static ServiceProvider BuildAppleProvider()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
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
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDigitalCardsInfrastructure(configuration);
        return services.BuildServiceProvider();
    }

    private static string HashToken(string token)
    {
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token))).ToLowerInvariant();
    }
}
