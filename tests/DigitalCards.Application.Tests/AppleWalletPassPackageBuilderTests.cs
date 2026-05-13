using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using DigitalCards.Domain;
using DigitalCards.Infrastructure.Branding;
using DigitalCards.Infrastructure.Wallets;
using Microsoft.Extensions.Options;

namespace DigitalCards.Application.Tests;

public sealed class AppleWalletPassPackageBuilderTests
{
    [Fact]
    public void BuildPassJson_ContainsStoreCardFields()
    {
        var builder = new AppleWalletPassPackageBuilder();
        var card = CreateCard();
        var client = CreateClient(card.ClientId);
        var business = CreateBusiness(card.BusinessId);

        var bytes = builder.BuildPassJson(
            card,
            client,
            business,
            CreateOptions(),
            "serial-123",
            new AppleWalletPassPackageBuilder.AppleWalletPassBuildSettings(
                "https://example.test/apple-wallet",
                "auth-token"));
        using var document = JsonDocument.Parse(bytes);
        var root = document.RootElement;

        Assert.Equal(1, root.GetProperty("formatVersion").GetInt32());
        Assert.Equal("pass.com.example.digitalcards", root.GetProperty("passTypeIdentifier").GetString());
        Assert.Equal("TEAMID1234", root.GetProperty("teamIdentifier").GetString());
        Assert.Equal("serial-123", root.GetProperty("serialNumber").GetString());
        Assert.Equal("https://example.test/apple-wallet", root.GetProperty("webServiceURL").GetString());
        Assert.Equal("auth-token", root.GetProperty("authenticationToken").GetString());
        Assert.True(root.TryGetProperty("storeCard", out var storeCard));
        Assert.Equal("Demo Coffee", storeCard.GetProperty("primaryFields")[0].GetProperty("value").GetString());
        Assert.Equal("2", storeCard.GetProperty("secondaryFields")[1].GetProperty("value").GetString());
        Assert.Equal("maria-test", root.GetProperty("barcodes")[0].GetProperty("message").GetString());
    }

    [Fact]
    public void BuildManifest_HashesUnsignedPackageFilesOnly()
    {
        var builder = new AppleWalletPassPackageBuilder();
        var files = new Dictionary<string, byte[]>
        {
            ["pass.json"] = Encoding.UTF8.GetBytes("{}"),
            ["icon.png"] = [1, 2, 3],
            ["manifest.json"] = Encoding.UTF8.GetBytes("old"),
            ["signature"] = Encoding.UTF8.GetBytes("old-signature")
        };

        var manifest = builder.BuildManifest(files);

        Assert.Equal(2, manifest.Count);
        Assert.Equal(Sha1Hex(files["pass.json"]), manifest["pass.json"]);
        Assert.Equal(Sha1Hex(files["icon.png"]), manifest["icon.png"]);
        Assert.DoesNotContain("manifest.json", manifest.Keys);
        Assert.DoesNotContain("signature", manifest.Keys);
    }

    [Fact]
    public void BuildUnsignedFiles_EmbedsUploadedPngBusinessLogo()
    {
        var uploadRoot = Path.Combine(Path.GetTempPath(), $"digitalcards-logo-{Guid.NewGuid():N}");
        var assetsRoot = Path.Combine(uploadRoot, "assets");
        var card = CreateCard();
        var client = CreateClient(card.ClientId);
        var businessLogoFolder = Path.Combine(uploadRoot, card.BusinessId.ToString("N"));
        var logoBytes = TinyPng();
        try
        {
            Directory.CreateDirectory(assetsRoot);
            Directory.CreateDirectory(businessLogoFolder);
            File.WriteAllBytes(Path.Combine(assetsRoot, "icon.png"), TinyPng());
            File.WriteAllBytes(Path.Combine(businessLogoFolder, "logo.png"), logoBytes);

            var builder = new AppleWalletPassPackageBuilder(Options.Create(new BusinessLogoUploadOptions
            {
                Path = uploadRoot,
                RequestPath = "/uploads/business-logos"
            }));
            var options = new AppleWalletOptions
            {
                TeamIdentifier = "TEAMID1234",
                PassTypeIdentifier = "pass.com.example.digitalcards",
                OrganizationName = "DigitalCards",
                AssetsPath = assetsRoot
            };
            var business = new Business(
                card.BusinessId,
                "Demo Coffee",
                "demo@example.test",
                "hash",
                $"/uploads/business-logos/{card.BusinessId:N}/logo.png");

            var files = builder.BuildUnsignedFiles(card, client, business, options);

            Assert.True(files.ContainsKey("icon.png"));
            Assert.True(files.ContainsKey("logo.png"));
            Assert.True(files.ContainsKey("logo@2x.png"));
            Assert.Equal(logoBytes, files["logo.png"]);
            Assert.Equal(logoBytes, files["logo@2x.png"]);
        }
        finally
        {
            if (Directory.Exists(uploadRoot))
            {
                Directory.Delete(uploadRoot, recursive: true);
            }
        }
    }

    private static AppleWalletOptions CreateOptions()
    {
        return new AppleWalletOptions
        {
            TeamIdentifier = "TEAMID1234",
            PassTypeIdentifier = "pass.com.example.digitalcards",
            OrganizationName = "DigitalCards"
        };
    }

    private static byte[] TinyPng()
    {
        return Convert.FromBase64String(
            "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+/p9sAAAAASUVORK5CYII=");
    }

    private static LoyaltyCard CreateCard()
    {
        var card = new LoyaltyCard(
            Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
            Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"),
            DateTimeOffset.Parse("2026-05-11T00:00:00Z"));
        card.AddStamp(DateTimeOffset.Parse("2026-05-11T00:05:00Z"));
        return card;
    }

    private static Client CreateClient(Guid clientId)
    {
        return new Client(clientId, "maria-test", "Maria", "Lopez", "maria@example.test");
    }

    private static Business CreateBusiness(Guid businessId)
    {
        return new Business(businessId, "Demo Coffee", "demo@example.test", "hash", "logo.png");
    }

    private static string Sha1Hex(byte[] bytes)
    {
        return Convert.ToHexString(SHA1.HashData(bytes)).ToLowerInvariant();
    }
}
