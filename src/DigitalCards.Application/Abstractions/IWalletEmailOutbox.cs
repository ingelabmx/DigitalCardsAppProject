using DigitalCards.Application.Models;

namespace DigitalCards.Application.Abstractions;

public interface IWalletEmailOutbox
{
    Task<IReadOnlyList<WalletEnrollmentEmail>> ListAsync(CancellationToken cancellationToken = default);
}

