using DigitalCards.Domain;

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

    public List<LoyaltyCard> LoyaltyCards { get; } = [];
}

