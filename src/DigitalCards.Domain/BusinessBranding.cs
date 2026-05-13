namespace DigitalCards.Domain;

public sealed class BusinessBranding
{
    public BusinessBranding(
        Guid businessId,
        string publicName,
        string logoPath,
        string primaryColor,
        string secondaryColor,
        string programName,
        string programDescription,
        DateTimeOffset updatedAt,
        Guid? updatedByAdminUserId)
    {
        if (businessId == Guid.Empty)
        {
            throw new ArgumentException("Business id is required.", nameof(businessId));
        }

        BusinessId = businessId;
        PublicName = publicName.Trim();
        LogoPath = logoPath.Trim();
        PrimaryColor = primaryColor.Trim();
        SecondaryColor = secondaryColor.Trim();
        ProgramName = programName.Trim();
        ProgramDescription = programDescription.Trim();
        UpdatedAt = updatedAt;
        UpdatedByAdminUserId = updatedByAdminUserId;
    }

    public Guid BusinessId { get; }

    public string PublicName { get; }

    public string LogoPath { get; }

    public string PrimaryColor { get; }

    public string SecondaryColor { get; }

    public string ProgramName { get; }

    public string ProgramDescription { get; }

    public DateTimeOffset UpdatedAt { get; }

    public Guid? UpdatedByAdminUserId { get; }
}
