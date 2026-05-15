using System.Reflection;
using DigitalCards.Domain;
using DigitalCards.Infrastructure;
using DigitalCards.Infrastructure.Wallets;
using Google.Apis.Walletobjects.v1.Data;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace DigitalCards.Application.Tests;

public sealed class GoogleWalletServiceTests
{
    [Fact]
    public void BuildObject_UsesBusinessLogoAndAppleAlignedFields()
    {
        var service = new GoogleWalletService(
            Options.Create(new GoogleWalletOptions
            {
                LogoImageUri = "https://fallback.example.test/logo.png"
            }),
            Options.Create(new DigitalCardsInfrastructureOptions
            {
                PublicBaseUrl = "https://app.puntelio.com"
            }),
            NullLogger<GoogleWalletService>.Instance);
        var card = CreateCard();
        var client = CreateClient(card.ClientId);
        var business = new Business(
            card.BusinessId,
            "Balboa Water",
            "balboa@example.test",
            "hash",
            "/uploads/business-logos/cccccccccccccccccccccccccccccccc/version-token/logo.png",
            publicName: "Runni Cafe",
            programName: "Runni Rewards",
            programDescription: "Cafe gratis al completar sellos.");

        var genericObject = BuildObject(service, card, client, business);

        Assert.Equal(
            "https://app.puntelio.com/uploads/business-logos/cccccccccccccccccccccccccccccccc/version-token/logo.png",
            genericObject.Logo.SourceUri.Uri);
        Assert.Equal("Runni Cafe", genericObject.CardTitle.DefaultValue.Value);
        Assert.Equal("Runni Cafe", genericObject.Header.DefaultValue.Value);
        Assert.Equal("Runni Rewards", genericObject.Subheader.DefaultValue.Value);
        Assert.Equal("maria-test", genericObject.Barcode.Value);

        Assert.Collection(
            genericObject.TextModulesData,
            module =>
            {
                Assert.Equal("client", module.Id);
                Assert.Equal("Cliente", module.Header);
                Assert.Equal("Maria Lopez", module.Body);
            },
            module =>
            {
                Assert.Equal("checks", module.Id);
                Assert.Equal("Sellos", module.Header);
                Assert.Equal("1 de 10", module.Body);
            },
            module =>
            {
                Assert.Equal("reward", module.Id);
                Assert.Equal("Recompensa", module.Header);
                Assert.Equal("Cafe gratis al completar sellos.", module.Body);
            });
        Assert.DoesNotContain(genericObject.TextModulesData, module => module.Header == "Sellos historicos");
        Assert.DoesNotContain(genericObject.TextModulesData, module => module.Header == "Fecha de alta");
    }

    [Fact]
    public void BuildClass_UsesClientChecksRewardTemplate()
    {
        var genericClass = BuildClass();

        var row = Assert.Single(genericClass.ClassTemplateInfo.CardTemplateOverride.CardRowTemplateInfos);
        Assert.Equal(
            "object.textModulesData['client']",
            row.ThreeItems.StartItem.FirstValue.Fields[0].FieldPath);
        Assert.Equal(
            "object.textModulesData['checks']",
            row.ThreeItems.MiddleItem.FirstValue.Fields[0].FieldPath);
        Assert.Equal(
            "object.textModulesData['reward']",
            row.ThreeItems.EndItem.FirstValue.Fields[0].FieldPath);
    }

    private static GenericObject BuildObject(
        GoogleWalletService service,
        LoyaltyCard card,
        Client client,
        Business business)
    {
        var method = typeof(GoogleWalletService).GetMethod(
            "BuildObject",
            BindingFlags.Instance | BindingFlags.NonPublic) ??
            throw new InvalidOperationException("BuildObject method was not found.");

        return (GenericObject)method.Invoke(
            service,
            ["issuer.object", "issuer.class", card, client, business])!;
    }

    private static GenericClass BuildClass()
    {
        var method = typeof(GoogleWalletService).GetMethod(
            "BuildClass",
            BindingFlags.Static | BindingFlags.NonPublic) ??
            throw new InvalidOperationException("BuildClass method was not found.");

        return (GenericClass)method.Invoke(null, ["issuer.class"])!;
    }

    private static LoyaltyCard CreateCard()
    {
        return new LoyaltyCard(
            Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
            Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"),
            DateTimeOffset.Parse("2026-05-11T00:00:00Z"));
    }

    private static Client CreateClient(Guid clientId)
    {
        return new Client(clientId, "maria-test", "Maria Fernanda", "Lopez Perez", "maria@example.test");
    }
}

