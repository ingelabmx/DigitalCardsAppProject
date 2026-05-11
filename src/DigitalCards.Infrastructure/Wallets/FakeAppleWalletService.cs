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
            DownloadUrl: null));
    }
}
