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
    public void AddStamp_KeepsCardCompleteUntilRewardIsRedeemed()
    {
        var now = DateTimeOffset.Parse("2026-05-08T12:00:00Z");
        var card = new LoyaltyCard(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), now);

        for (var i = 0; i < 4; i++)
        {
            card.AddStamp(now.AddMinutes(i + 1), stampGoal: 5);
        }

        Assert.Equal(5, card.CurrentStamps);

        card.AddStamp(now.AddMinutes(10), stampGoal: 5);

        Assert.Equal(5, card.CurrentStamps);
        Assert.Equal(5, card.LifetimeStamps);
    }

    [Fact]
    public void RedeemReward_ResetsVisibleStampsAndKeepsLifetime()
    {
        var now = DateTimeOffset.Parse("2026-05-08T12:00:00Z");
        var card = new LoyaltyCard(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), now);

        for (var i = 0; i < 4; i++)
        {
            card.AddStamp(now.AddMinutes(i + 1), stampGoal: 5);
        }

        card.RedeemReward(now.AddMinutes(10), stampGoal: 5);

        Assert.Equal(0, card.CurrentStamps);
        Assert.Equal(5, card.LifetimeStamps);
        Assert.Equal(now.AddMinutes(10), card.LastStampedAt);
    }

    [Fact]
    public void Restore_RehydratesPersistedWalletState()
    {
        var id = Guid.NewGuid();
        var clientId = Guid.NewGuid();
        var businessId = Guid.NewGuid();
        var createdAt = DateTimeOffset.Parse("2026-05-08T12:00:00Z");
        var lastStampedAt = createdAt.AddHours(2);

        var card = LoyaltyCard.Restore(
            id,
            clientId,
            businessId,
            "token-123",
            4,
            13,
            createdAt,
            lastStampedAt,
            "google-object",
            "https://wallet.example.test/save");

        Assert.Equal(id, card.Id);
        Assert.Equal(clientId, card.ClientId);
        Assert.Equal(businessId, card.BusinessId);
        Assert.Equal("token-123", card.EnrollmentToken);
        Assert.Equal(4, card.CurrentStamps);
        Assert.Equal(13, card.LifetimeStamps);
        Assert.Equal(lastStampedAt, card.LastStampedAt);
        Assert.Equal("google-object", card.GoogleObjectId);
    }
}
