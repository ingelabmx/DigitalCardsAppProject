namespace DigitalCards.Domain;

public sealed class LoyaltyCard
{
    private const int MaxVisibleStamps = 9;

    public LoyaltyCard(Guid id, Guid clientId, Guid businessId, DateTimeOffset createdAt)
    {
        Id = id;
        ClientId = clientId;
        BusinessId = businessId;
        CreatedAt = createdAt;
        LastStampedAt = createdAt;
        CurrentStamps = 1;
        LifetimeStamps = 1;
        EnrollmentToken = id.ToString("N");
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

    public void AddStamp(DateTimeOffset stampedAt)
    {
        CurrentStamps = CurrentStamps >= MaxVisibleStamps ? 0 : CurrentStamps + 1;
        LifetimeStamps++;
        LastStampedAt = stampedAt;
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

