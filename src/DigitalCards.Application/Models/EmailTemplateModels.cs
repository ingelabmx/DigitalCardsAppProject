namespace DigitalCards.Application.Models;

public enum EmailTemplateKind
{
    WalletEnrollment,
    Welcome,
    PasswordReset,
    InternalAlert
}

public sealed record RenderedEmailTemplate(
    EmailTemplateKind Kind,
    string To,
    string Subject,
    string TextBody,
    string HtmlBody);

public sealed record EmailBranding(
    string DisplayName,
    string? LogoUrl = null,
    string? PrimaryColor = null,
    string? ProgramName = null);

public sealed record WelcomeEmail(
    string To,
    string RecipientName,
    string LoginUrl,
    DateTimeOffset CreatedAt,
    EmailBranding? Branding = null);

public sealed record PasswordResetEmail(
    string To,
    string RecipientName,
    string AccountType,
    string ResetUrl,
    DateTimeOffset ExpiresAt,
    DateTimeOffset CreatedAt,
    EmailBranding? Branding = null);

public sealed record InternalAlertEmail(
    string To,
    string AlertTitle,
    string AlertSummary,
    string Severity,
    string? ActionUrl,
    DateTimeOffset CreatedAt);
