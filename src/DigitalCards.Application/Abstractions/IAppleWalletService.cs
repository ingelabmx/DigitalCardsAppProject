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
}
