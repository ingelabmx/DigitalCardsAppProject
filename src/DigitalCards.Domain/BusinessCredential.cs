namespace DigitalCards.Domain;

public sealed class BusinessCredential
{
    public BusinessCredential(
        Guid businessId,
        string passwordHash,
        DateTimeOffset createdAt,
        DateTimeOffset updatedAt)
    {
        if (businessId == Guid.Empty)
        {
            throw new ArgumentException("Business id is required.", nameof(businessId));
        }

        if (string.IsNullOrWhiteSpace(passwordHash))
        {
            throw new ArgumentException("Password hash is required.", nameof(passwordHash));
        }

        BusinessId = businessId;
        PasswordHash = passwordHash;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
    }

    public Guid BusinessId { get; }

    public string PasswordHash { get; }

    public DateTimeOffset CreatedAt { get; }

    public DateTimeOffset UpdatedAt { get; }

    public BusinessCredential Rehash(string passwordHash, DateTimeOffset updatedAt)
    {
        return new BusinessCredential(BusinessId, passwordHash, CreatedAt, updatedAt);
    }
}
