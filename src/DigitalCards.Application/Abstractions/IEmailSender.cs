using DigitalCards.Application.Models;

namespace DigitalCards.Application.Abstractions;

public interface IEmailSender
{
    Task SendWalletEnrollmentAsync(WalletEnrollmentEmail email, CancellationToken cancellationToken = default);

    Task SendPasswordResetAsync(PasswordResetEmail email, CancellationToken cancellationToken = default);
}
