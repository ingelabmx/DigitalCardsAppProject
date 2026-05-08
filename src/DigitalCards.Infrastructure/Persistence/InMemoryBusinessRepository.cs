using DigitalCards.Application.Abstractions;
using DigitalCards.Domain;

namespace DigitalCards.Infrastructure.Persistence;

public sealed class InMemoryBusinessRepository : IBusinessRepository
{
    private readonly InMemoryDigitalCardsStore _store;

    public InMemoryBusinessRepository(InMemoryDigitalCardsStore store)
    {
        _store = store;
    }

    public Task<Business?> FindByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_store.Businesses.SingleOrDefault(
            business => string.Equals(business.Email, email.Trim(), StringComparison.OrdinalIgnoreCase)));
    }

    public Task<Business?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_store.Businesses.SingleOrDefault(business => business.Id == id));
    }

    public Task<IReadOnlyList<Business>> ListAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<Business>>(_store.Businesses.ToArray());
    }
}

