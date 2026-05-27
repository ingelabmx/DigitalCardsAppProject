using DigitalCards.Application.Models;

namespace DigitalCards.Application.Abstractions;

public interface IEmailSender
{
    Task SendWalletEnrollmentAsync(WalletEnrollmentEmail email, CancellationToken cancellationToken = default);

    Task SendPasswordResetAsync(PasswordResetEmail email, CancellationToken cancellationToken = default);

    Task SendLandingContactAsync(LandingContactEmail email, CancellationToken cancellationToken = default);

    Task SendPasswordChangedAsync(PasswordChangedEmail email, CancellationToken cancellationToken = default);

    Task SendBusinessWelcomeAsync(BusinessWelcomeEmail email, CancellationToken cancellationToken = default);

    Task SendPaymentFailedAsync(BusinessPaymentFailedEmail email, CancellationToken cancellationToken = default);

    Task SendSubscriptionCanceledAsync(BusinessSubscriptionCanceledEmail email, CancellationToken cancellationToken = default);
}
