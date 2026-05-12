using DigitalCards.Domain;

namespace DigitalCards.Application.Abstractions;

public interface IAdminUserRepository
{
    Task<AdminUser?> FindByUserNameOrEmailAsync(
        string value,
        CancellationToken cancellationToken = default);
}
