using DigitalCards.Application.Models;
using DigitalCards.Domain;

namespace DigitalCards.Application.Abstractions;

public interface IGoogleWalletService
{
    Task<GoogleWalletIssueResult> IssueSaveLinkAsync(
        LoyaltyCard card,
        Client client,
        Business business,
        CancellationToken cancellationToken = default);

    Task PatchStampStateAsync(
        LoyaltyCard card,
        Client client,
        Business business,
        CancellationToken cancellationToken = default);
}
