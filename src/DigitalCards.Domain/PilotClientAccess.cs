namespace DigitalCards.Domain;

public sealed class PilotClientAccess
{
    public PilotClientAccess(
        Guid clientId,
        bool isEnabled,
        string? notes,
        DateTimeOffset createdAt,
        DateTimeOffset updatedAt,
        Guid updatedByAdminUserId)
    {
        if (clientId == Guid.Empty)
        {
            throw new ArgumentException("Client id is required.", nameof(clientId));
        }

        if (updatedByAdminUserId == Guid.Empty)
        {
            throw new ArgumentException("Admin user id is required.", nameof(updatedByAdminUserId));
        }

        ClientId = clientId;
        IsEnabled = isEnabled;
        Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
        UpdatedByAdminUserId = updatedByAdminUserId;
    }

    public Guid ClientId { get; }

    public bool IsEnabled { get; }

    public string? Notes { get; }

    public DateTimeOffset CreatedAt { get; }

    public DateTimeOffset UpdatedAt { get; }

    public Guid UpdatedByAdminUserId { get; }

    public PilotClientAccess WithState(
        bool isEnabled,
        string? notes,
        DateTimeOffset updatedAt,
        Guid updatedByAdminUserId)
    {
        return new PilotClientAccess(
            ClientId,
            isEnabled,
            notes,
            CreatedAt,
            updatedAt,
            updatedByAdminUserId);
    }
}
