namespace DigitalCards.Domain;

public sealed class PilotBusinessAccess
{
    public PilotBusinessAccess(
        Guid businessId,
        bool isEnabled,
        string? notes,
        DateTimeOffset createdAt,
        DateTimeOffset updatedAt,
        Guid updatedByAdminUserId)
    {
        if (businessId == Guid.Empty)
        {
            throw new ArgumentException("Business id is required.", nameof(businessId));
        }

        if (updatedByAdminUserId == Guid.Empty)
        {
            throw new ArgumentException("Admin user id is required.", nameof(updatedByAdminUserId));
        }

        BusinessId = businessId;
        IsEnabled = isEnabled;
        Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
        UpdatedByAdminUserId = updatedByAdminUserId;
    }

    public Guid BusinessId { get; }

    public bool IsEnabled { get; }

    public string? Notes { get; }

    public DateTimeOffset CreatedAt { get; }

    public DateTimeOffset UpdatedAt { get; }

    public Guid UpdatedByAdminUserId { get; }

    public PilotBusinessAccess WithState(
        bool isEnabled,
        string? notes,
        DateTimeOffset updatedAt,
        Guid updatedByAdminUserId)
    {
        return new PilotBusinessAccess(
            BusinessId,
            isEnabled,
            notes,
            CreatedAt,
            updatedAt,
            updatedByAdminUserId);
    }
}
