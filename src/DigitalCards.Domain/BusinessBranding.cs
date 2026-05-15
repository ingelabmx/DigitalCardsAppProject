namespace DigitalCards.Domain;

public sealed class BusinessBranding
{
    public BusinessBranding(
        Guid businessId,
        string publicName,
        string logoPath,
        string primaryColor,
        string secondaryColor,
        string customFieldColor,
        int stampGoal,
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
        CustomFieldColor = customFieldColor.Trim();
        StampGoal = stampGoal > 0 ? stampGoal : throw new ArgumentOutOfRangeException(nameof(stampGoal), "Stamp goal must be greater than zero.");
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

    public string CustomFieldColor { get; }

    public int StampGoal { get; }

    public string ProgramName { get; }

    public string ProgramDescription { get; }

    public DateTimeOffset UpdatedAt { get; }

    public Guid? UpdatedByAdminUserId { get; }
}
