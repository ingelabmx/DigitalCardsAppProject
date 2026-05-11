using DigitalCards.Application.Abstractions;
using DigitalCards.Application.Models;
using DigitalCards.Domain;
using Microsoft.Extensions.Logging;

namespace DigitalCards.Infrastructure.Wallets;

public sealed class FakeAppleWalletService : IAppleWalletService
{
    private readonly ILogger<FakeAppleWalletService> _logger;

    public FakeAppleWalletService(ILogger<FakeAppleWalletService> logger)
    {
        _logger = logger;
    }

    public Task<AppleWalletIssueResult> IssueAsync(
        LoyaltyCard card,
        Client client,
        Business business,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Apple Wallet is pending for card {CardId} and business {BusinessId}.",
            card.Id,
            business.Id);

        return Task.FromResult(new AppleWalletIssueResult(
            AppleWalletIssueStatus.Pending,
            "Apple Wallet todavia no genera archivos .pkpass. Este flujo ya pasa por el contrato de Application para conectar la implementacion real en una fase posterior.",
            DownloadUrl: null,
            SerialNumber: null));
    }

    public Task<AppleWalletPassFile> CreatePassAsync(
        LoyaltyCard card,
        Client client,
        Business business,
        CancellationToken cancellationToken = default)
    {
        throw new InvalidOperationException("Apple Wallet pass download is not available when DigitalCards:AppleWallet:Provider is Fake.");
    }

    public Task<AppleWalletPassRequestResult> CreateUpdatedPassAsync(
        string passTypeIdentifier,
        string serialNumber,
        string? authorizationHeader,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new AppleWalletPassRequestResult(
            AppleWalletPassRequestStatus.Unauthorized,
            PassFile: null));
    }

    public Task<AppleWalletRegistrationStatus> RegisterDeviceAsync(
        string deviceLibraryIdentifier,
        string passTypeIdentifier,
        string serialNumber,
        string pushToken,
        string? authorizationHeader,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(AppleWalletRegistrationStatus.Unauthorized);
    }

    public Task<AppleWalletUnregistrationStatus> UnregisterDeviceAsync(
        string deviceLibraryIdentifier,
        string passTypeIdentifier,
        string serialNumber,
        string? authorizationHeader,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(AppleWalletUnregistrationStatus.Unauthorized);
    }

    public Task<AppleWalletUpdatedPasses?> ListUpdatedPassesAsync(
        string deviceLibraryIdentifier,
        string passTypeIdentifier,
        string? previousLastUpdated,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult<AppleWalletUpdatedPasses?>(null);
    }

    public Task NotifyPassUpdatedAsync(
        LoyaltyCard card,
        Client client,
        Business business,
        CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}
