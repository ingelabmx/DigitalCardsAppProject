using DigitalCards.Domain;

namespace DigitalCards.Application.Abstractions;

public interface IAdminUserRepository
{
    Task<AdminUser> AddAsync(
        AdminUser admin,
        CancellationToken cancellationToken = default);

    Task<AdminUser?> FindByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<AdminUser?> FindByUserNameOrEmailAsync(
        string value,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AdminUser>> ListAsync(CancellationToken cancellationToken = default);

    Task<AdminUser> UpdatePasswordAsync(
        Guid id,
        string legacyPasswordHash,
        CancellationToken cancellationToken = default);
}
