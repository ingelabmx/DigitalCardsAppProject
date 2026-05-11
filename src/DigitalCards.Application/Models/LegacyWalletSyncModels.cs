using DigitalCards.Domain;

namespace DigitalCards.Application.Models;

public sealed record LegacyWalletSyncCandidate(
    LoyaltyCard Card,
    Client Client,
    Business Business,
    bool HasRegisteredAppleDevices);
