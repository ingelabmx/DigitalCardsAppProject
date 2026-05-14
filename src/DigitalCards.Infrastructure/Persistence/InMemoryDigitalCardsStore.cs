using DigitalCards.Domain;
using DigitalCards.Application.Models;

namespace DigitalCards.Infrastructure.Persistence;

public sealed class InMemoryDigitalCardsStore
{
    public InMemoryDigitalCardsStore()
    {
        AdminUsers.Add(new AdminUser(
            Guid.Parse("00000000-0000-0000-0000-000000000001"),
            "DCAdmin",
            "DigitalCards",
            "Admin",
            "admin@digitalcards.test",
            "admin123"));

        Businesses.Add(new Business(
            Guid.Parse("11111111-1111-1111-1111-111111111111"),
            "Demo Coffee",
            "demo@digitalcards.test",
            "business123",
            "/img/demo-coffee.svg"));
    }

    public object Sync { get; } = new();

    public List<Client> Clients { get; } = [];

    public List<AdminUser> AdminUsers { get; } = [];

    public List<AdminCredential> AdminCredentials { get; } = [];

    public List<Business> Businesses { get; } = [];

    public List<BusinessBranding> BusinessBranding { get; } = [];

    public List<PilotBusinessAccess> PilotBusinesses { get; } = [];

    public List<PilotClientAccess> PilotClients { get; } = [];

    public List<BusinessCredential> BusinessCredentials { get; } = [];

    public List<ClientCredential> ClientCredentials { get; } = [];

    public List<LoyaltyCard> LoyaltyCards { get; } = [];

    public List<ClientCardLifecycleRecord> ClientCardStatuses { get; } = [];

    public List<AppleWalletPassRecord> AppleWalletPasses { get; } = [];

    public List<AppleWalletDeviceRecord> AppleWalletDevices { get; } = [];

    public List<(string DeviceLibraryIdentifier, string PassTypeIdentifier, string SerialNumber, DateTimeOffset CreatedAt)> AppleWalletRegistrations { get; } = [];

    public List<WalletLinkTokenRecord> WalletLinkTokens { get; } = [];

    public List<BusinessEnrollmentLinkRecord> BusinessEnrollmentLinks { get; } = [];

    public List<PasswordResetTokenRecord> PasswordResetTokens { get; } = [];

    public List<StampLedgerRecord> StampLedger { get; } = [];

    public List<OperationalAuditEvent> AuditEvents { get; } = [];

    public List<ClientConsent> ClientConsents { get; } = [];

    public List<CutoverSmokeEvidence> CutoverSmokeEvidence { get; } = [];
}
