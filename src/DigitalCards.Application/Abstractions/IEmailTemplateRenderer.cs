using DigitalCards.Application.Models;

namespace DigitalCards.Application.Abstractions;

public interface IEmailTemplateRenderer
{
    RenderedEmailTemplate RenderWalletEnrollment(WalletEnrollmentEmail email);

    RenderedEmailTemplate RenderWelcome(WelcomeEmail email);

    RenderedEmailTemplate RenderPasswordReset(PasswordResetEmail email);

    RenderedEmailTemplate RenderInternalAlert(InternalAlertEmail email);
}
