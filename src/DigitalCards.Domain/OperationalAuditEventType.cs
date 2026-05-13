namespace DigitalCards.Domain;

public enum OperationalAuditEventType
{
    AdminCreated = 1,
    AdminPasswordReset = 2,
    BusinessCreated = 3,
    BusinessUpdated = 4,
    BusinessPasswordReset = 5,
    BusinessBrandingUpdated = 6,
    PilotBusinessChanged = 7,
    CutoverStatusChanged = 8,
    PilotClientChanged = 9,
    SupportExported = 10,
    WalletRetryRequested = 11,
    BusinessEnrollmentLinkGenerated = 12
}
