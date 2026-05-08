using DigitalCards.Domain;

namespace DigitalCards.Domain.Tests;

public sealed class LoyaltyCardTests
{
    [Fact]
    public void AddStamp_IncrementsVisibleAndLifetimeStamps()
    {
        var now = DateTimeOffset.Parse("2026-05-08T12:00:00Z");
        var card = new LoyaltyCard(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), now);

        card.AddStamp(now.AddMinutes(5));

        Assert.Equal(2, card.CurrentStamps);
        Assert.Equal(2, card.LifetimeStamps);
        Assert.Equal(now.AddMinutes(5), card.LastStampedAt);
    }

    [Fact]
    public void AddStamp_ResetsVisibleStampsAfterNineAndKeepsLifetime()
    {
        var now = DateTimeOffset.Parse("2026-05-08T12:00:00Z");
        var card = new LoyaltyCard(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), now);

        for (var i = 0; i < 8; i++)
        {
            card.AddStamp(now.AddMinutes(i + 1));
        }

        Assert.Equal(9, card.CurrentStamps);

        card.AddStamp(now.AddMinutes(10));

        Assert.Equal(0, card.CurrentStamps);
        Assert.Equal(10, card.LifetimeStamps);
    }
}

