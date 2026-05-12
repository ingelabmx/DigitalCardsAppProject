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

    public Task<Business> AddAsync(Business business, CancellationToken cancellationToken = default)
    {
        lock (_store.Sync)
        {
            if (_store.Businesses.Any(existing =>
                string.Equals(existing.Name, business.Name, StringComparison.OrdinalIgnoreCase)))
            {
                throw new InvalidOperationException("A business with the same name or email already exists.");
            }

            if (_store.Businesses.Any(existing =>
                string.Equals(existing.Email, business.Email, StringComparison.OrdinalIgnoreCase)))
            {
                throw new InvalidOperationException("A business with the same name or email already exists.");
            }

            _store.Businesses.Add(business);
            return Task.FromResult(business);
        }
    }

    public Task<Business?> FindByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        lock (_store.Sync)
        {
            return Task.FromResult(_store.Businesses.SingleOrDefault(
                business => string.Equals(business.Email, email.Trim(), StringComparison.OrdinalIgnoreCase)));
        }
    }

    public Task<Business?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        lock (_store.Sync)
        {
            return Task.FromResult(_store.Businesses.SingleOrDefault(business => business.Id == id));
        }
    }

    public Task<Business?> FindByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        lock (_store.Sync)
        {
            return Task.FromResult(_store.Businesses.SingleOrDefault(
                business => string.Equals(business.Name, name.Trim(), StringComparison.OrdinalIgnoreCase)));
        }
    }

    public Task<IReadOnlyList<Business>> ListAsync(CancellationToken cancellationToken = default)
    {
        lock (_store.Sync)
        {
            return Task.FromResult<IReadOnlyList<Business>>(_store.Businesses.ToArray());
        }
    }
}
