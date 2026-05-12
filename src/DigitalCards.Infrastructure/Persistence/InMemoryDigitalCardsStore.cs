using DigitalCards.Domain;
using DigitalCards.Application.Models;

namespace DigitalCards.Infrastructure.Persistence;

public sealed class InMemoryDigitalCardsStore
{
    public InMemoryDigitalCardsStore()
    {
        Businesses.Add(new Business(
            Guid.Parse("11111111-1111-1111-1111-111111111111"),
            "Demo Coffee",
            "demo@digitalcards.test",
            "business123",
            "/img/demo-coffee.svg"));
    }

    public object Sync { get; } = new();

    public List<Client> Clients { get; } = [];

    public List<Business> Businesses { get; } = [];

    public List<BusinessCredential> BusinessCredentials { get; } = [];

    public List<LoyaltyCard> LoyaltyCards { get; } = [];

    public List<AppleWalletPassRecord> AppleWalletPasses { get; } = [];

    public List<AppleWalletDeviceRecord> AppleWalletDevices { get; } = [];

    public List<(string DeviceLibraryIdentifier, string PassTypeIdentifier, string SerialNumber, DateTimeOffset CreatedAt)> AppleWalletRegistrations { get; } = [];

    public List<WalletLinkTokenRecord> WalletLinkTokens { get; } = [];

    public List<StampLedgerRecord> StampLedger { get; } = [];
}
