using DigitalCards.Domain;

namespace DigitalCards.Application.Abstractions;

public interface IClientRepository
{
    Task AddAsync(Client client, CancellationToken cancellationToken = default);

    Task<Client> UpdatePasswordAsync(
        Guid clientId,
        string legacyPasswordHash,
        CancellationToken cancellationToken = default);

    Task<Client?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Client?> FindByUserNameOrEmailAsync(string value, CancellationToken cancellationToken = default);

    Task<bool> UserNameOrEmailExistsAsync(string value, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Client>> SearchAsync(
        string query,
        CancellationToken cancellationToken = default);
}
