using DigitalCards.Domain;

namespace DigitalCards.Application.Abstractions;

public interface IAdminCredentialRepository
{
    Task<AdminCredential?> FindByAdminUserIdAsync(
        Guid adminUserId,
        CancellationToken cancellationToken = default);

    Task UpsertAsync(
        AdminCredential credential,
        CancellationToken cancellationToken = default);
}
