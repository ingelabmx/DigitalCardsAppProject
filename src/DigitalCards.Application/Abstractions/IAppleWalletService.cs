using DigitalCards.Application.Models;
using DigitalCards.Domain;

namespace DigitalCards.Application.Abstractions;

public interface IAppleWalletService
{
    Task<AppleWalletIssueResult> IssueAsync(
        LoyaltyCard card,
        Client client,
        Business business,
        CancellationToken cancellationToken = default);

    Task<AppleWalletPassFile> CreatePassAsync(
        LoyaltyCard card,
        Client client,
        Business business,
        CancellationToken cancellationToken = default);

    Task<AppleWalletPassRequestResult> CreateUpdatedPassAsync(
        string passTypeIdentifier,
        string serialNumber,
        string? authorizationHeader,
        CancellationToken cancellationToken = default);

    Task<AppleWalletRegistrationStatus> RegisterDeviceAsync(
        string deviceLibraryIdentifier,
        string passTypeIdentifier,
        string serialNumber,
        string pushToken,
        string? authorizationHeader,
        CancellationToken cancellationToken = default);

    Task<AppleWalletUnregistrationStatus> UnregisterDeviceAsync(
        string deviceLibraryIdentifier,
        string passTypeIdentifier,
        string serialNumber,
        string? authorizationHeader,
        CancellationToken cancellationToken = default);

    Task<AppleWalletUpdatedPasses?> ListUpdatedPassesAsync(
        string deviceLibraryIdentifier,
        string passTypeIdentifier,
        string? previousLastUpdated,
        CancellationToken cancellationToken = default);

    Task NotifyPassUpdatedAsync(
        LoyaltyCard card,
        Client client,
        Business business,
        CancellationToken cancellationToken = default);
}
