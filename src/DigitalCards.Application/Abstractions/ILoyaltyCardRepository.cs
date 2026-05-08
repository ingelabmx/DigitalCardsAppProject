using DigitalCards.Domain;

namespace DigitalCards.Application.Abstractions;

public interface ILoyaltyCardRepository
{
    Task AddAsync(LoyaltyCard card, CancellationToken cancellationToken = default);

    Task UpdateAsync(LoyaltyCard card, CancellationToken cancellationToken = default);

    Task<LoyaltyCard?> FindByClientAndBusinessAsync(Guid clientId, Guid businessId, CancellationToken cancellationToken = default);

    Task<LoyaltyCard?> FindByEnrollmentTokenAsync(string token, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<LoyaltyCard>> ListByClientAsync(Guid clientId, CancellationToken cancellationToken = default);
}
