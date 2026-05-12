namespace DigitalCards.Domain;

public sealed class AdminCredential
{
    public AdminCredential(
        Guid adminUserId,
        string passwordHash,
        DateTimeOffset createdAt,
        DateTimeOffset updatedAt)
    {
        if (adminUserId == Guid.Empty)
        {
            throw new ArgumentException("Admin user id is required.", nameof(adminUserId));
        }

        if (string.IsNullOrWhiteSpace(passwordHash))
        {
            throw new ArgumentException("Password hash is required.", nameof(passwordHash));
        }

        AdminUserId = adminUserId;
        PasswordHash = passwordHash;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
    }

    public Guid AdminUserId { get; }

    public string PasswordHash { get; }

    public DateTimeOffset CreatedAt { get; }

    public DateTimeOffset UpdatedAt { get; }

    public AdminCredential Rehash(string passwordHash, DateTimeOffset updatedAt)
    {
        return new AdminCredential(AdminUserId, passwordHash, CreatedAt, updatedAt);
    }
}
