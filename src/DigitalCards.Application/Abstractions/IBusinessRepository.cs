using DigitalCards.Domain;

namespace DigitalCards.Application.Abstractions;

public interface IBusinessRepository
{
    Task<Business> AddAsync(Business business, CancellationToken cancellationToken = default);

    Task<Business?> FindByEmailAsync(string email, CancellationToken cancellationToken = default);

    Task<Business?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Business?> FindByNameAsync(string name, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Business>> ListAsync(CancellationToken cancellationToken = default);
}
