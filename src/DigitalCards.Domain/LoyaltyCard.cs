namespace DigitalCards.Domain;

public sealed class LoyaltyCard
{
    private const int MaxVisibleStamps = 1000;

    public LoyaltyCard(Guid id, Guid clientId, Guid businessId, DateTimeOffset createdAt)
        : this(
            id,
            clientId,
            businessId,
            id.ToString("N"),
            currentStamps: 0,
            lifetimeStamps: 0,
            createdAt,
            lastStampedAt: createdAt,
            googleObjectId: null,
            googleSaveUrl: null)
    {
    }

    private LoyaltyCard(
        Guid id,
        Guid clientId,
        Guid businessId,
        string enrollmentToken,
        int currentStamps,
        int lifetimeStamps,
        DateTimeOffset createdAt,
        DateTimeOffset lastStampedAt,
        string? googleObjectId,
        string? googleSaveUrl)
    {
        Id = id;
        ClientId = clientId;
        BusinessId = businessId;
        EnrollmentToken = enrollmentToken;
        CurrentStamps = currentStamps;
        LifetimeStamps = lifetimeStamps;
        CreatedAt = createdAt;
        LastStampedAt = lastStampedAt;
        GoogleObjectId = googleObjectId;
        GoogleSaveUrl = googleSaveUrl;
    }

    public Guid Id { get; }

    public Guid ClientId { get; }

    public Guid BusinessId { get; }

    public string EnrollmentToken { get; }

    public int CurrentStamps { get; private set; }

    public int LifetimeStamps { get; private set; }

    public DateTimeOffset CreatedAt { get; }

    public DateTimeOffset LastStampedAt { get; private set; }

    public string? GoogleObjectId { get; private set; }

    public string? GoogleSaveUrl { get; private set; }

    public static LoyaltyCard Restore(
        Guid id,
        Guid clientId,
        Guid businessId,
        string enrollmentToken,
        int currentStamps,
        int lifetimeStamps,
        DateTimeOffset createdAt,
        DateTimeOffset lastStampedAt,
        string? googleObjectId,
        string? googleSaveUrl)
    {
        if (string.IsNullOrWhiteSpace(enrollmentToken))
        {
            throw new ArgumentException("Enrollment token is required.", nameof(enrollmentToken));
        }

        if (currentStamps < 0 || currentStamps > MaxVisibleStamps)
        {
            throw new ArgumentOutOfRangeException(nameof(currentStamps), $"Current stamps must be between 0 and {MaxVisibleStamps}.");
        }

        if (lifetimeStamps < currentStamps)
        {
            throw new ArgumentOutOfRangeException(nameof(lifetimeStamps), "Lifetime stamps cannot be lower than current stamps.");
        }

        return new LoyaltyCard(
            id,
            clientId,
            businessId,
            enrollmentToken,
            currentStamps,
            lifetimeStamps,
            createdAt,
            lastStampedAt,
            googleObjectId,
            googleSaveUrl);
    }

    public void AddStamp(DateTimeOffset stampedAt, int stampGoal = Business.DefaultStampGoal)
    {
        var normalizedGoal = stampGoal > 0 ? Math.Min(stampGoal, MaxVisibleStamps) : Business.DefaultStampGoal;
        if (CurrentStamps >= normalizedGoal)
        {
            return;
        }

        CurrentStamps++;
        LifetimeStamps++;
        LastStampedAt = stampedAt;
    }

    public void RedeemReward(DateTimeOffset redeemedAt, int stampGoal = Business.DefaultStampGoal)
    {
        var normalizedGoal = stampGoal > 0 ? Math.Min(stampGoal, MaxVisibleStamps) : Business.DefaultStampGoal;
        if (CurrentStamps < normalizedGoal)
        {
            throw new InvalidOperationException("Reward cannot be redeemed until the card is complete.");
        }

        CurrentStamps = 0;
        LastStampedAt = redeemedAt;
    }

    public void MarkGoogleIssued(string objectId, string saveUrl)
    {
        if (string.IsNullOrWhiteSpace(objectId))
        {
            throw new ArgumentException("Google object id is required.", nameof(objectId));
        }

        if (string.IsNullOrWhiteSpace(saveUrl))
        {
            throw new ArgumentException("Google save URL is required.", nameof(saveUrl));
        }

        GoogleObjectId = objectId;
        GoogleSaveUrl = saveUrl;
    }
}
