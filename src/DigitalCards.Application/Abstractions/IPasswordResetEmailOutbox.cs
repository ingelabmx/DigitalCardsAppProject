using DigitalCards.Application.Models;

namespace DigitalCards.Application.Abstractions;

public interface IPasswordResetEmailOutbox
{
    Task<IReadOnlyList<PasswordResetEmail>> ListPasswordResetsAsync(
        CancellationToken cancellationToken = default);
}
