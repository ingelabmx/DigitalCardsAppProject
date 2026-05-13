namespace DigitalCards.Domain;

public sealed class ClientCredential
{
    public ClientCredential(
        Guid clientId,
        string passwordHash,
        DateTimeOffset createdAt,
        DateTimeOffset updatedAt)
    {
        if (clientId == Guid.Empty)
        {
            throw new ArgumentException("Client id is required.", nameof(clientId));
        }

        if (string.IsNullOrWhiteSpace(passwordHash))
        {
            throw new ArgumentException("Password hash is required.", nameof(passwordHash));
        }

        ClientId = clientId;
        PasswordHash = passwordHash;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
    }

    public Guid ClientId { get; }

    public string PasswordHash { get; }

    public DateTimeOffset CreatedAt { get; }

    public DateTimeOffset UpdatedAt { get; }

    public ClientCredential Rehash(string passwordHash, DateTimeOffset updatedAt)
    {
        return new ClientCredential(ClientId, passwordHash, CreatedAt, updatedAt);
    }
}
