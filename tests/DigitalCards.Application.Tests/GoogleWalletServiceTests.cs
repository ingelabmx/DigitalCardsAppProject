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
    public void BuildObject_UsesBusinessLogoPathAsPublicLogoUri()
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
            "Demo Coffee",
            "demo@example.test",
            "hash",
            "/uploads/business-logos/cccccccccccccccccccccccccccccccc/logo.png",
            publicName: "Puntelio Cafe");

        var genericObject = BuildObject(service, card, client, business);

        Assert.Equal(
            "https://app.puntelio.com/uploads/business-logos/cccccccccccccccccccccccccccccccc/logo.png",
            genericObject.Logo.SourceUri.Uri);
        Assert.Equal("Puntelio Cafe", genericObject.CardTitle.DefaultValue.Value);
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
        return new Client(clientId, "maria-test", "Maria", "Lopez", "maria@example.test");
    }
}

