using System.Net;
using DigitalCards.Application.Abstractions;
using DigitalCards.Application.Models;

namespace DigitalCards.Infrastructure.Email;

public sealed class EmailTemplateRenderer : IEmailTemplateRenderer
{
    private const string DefaultBrandName = "DigitalCards";
    private const string DefaultPrimaryColor = "#111827";

    public RenderedEmailTemplate RenderWalletEnrollment(WalletEnrollmentEmail email)
    {
        var brand = new EmailBranding(
            email.BusinessName,
            email.BusinessLogoUrl,
            email.PrimaryColor,
            email.ProgramName);
        var subject = $"Tu tarjeta digital de {email.BusinessName} esta lista";
        var textBody = $"""
            Hola {email.ClientName},

            Tu tarjeta digital de {email.BusinessName} esta lista.

            Abre este link para elegir Apple Wallet o Google Wallet:
            {email.EnrollmentUrl}

            Gracias por usar DigitalCards.
            """;
        var bodyHtml = $"""
            <h1>Tu tarjeta digital esta lista</h1>
            <p>Hola {Html(email.ClientName)},</p>
            <p>Tu tarjeta digital de <strong>{Html(email.BusinessName)}</strong> esta lista para agregarse a tu billetera digital.</p>
            <p>Elige Apple Wallet o Google Wallet desde el siguiente link:</p>
            {Button(email.EnrollmentUrl, "Abrir tarjeta digital", brand)}
            {FallbackLink(email.EnrollmentUrl)}
            """;

        return new RenderedEmailTemplate(
            EmailTemplateKind.WalletEnrollment,
            email.To,
            subject,
            textBody,
            Layout("Tu tarjeta digital esta lista", bodyHtml, brand));
    }

    public RenderedEmailTemplate RenderWelcome(WelcomeEmail email)
    {
        var brand = NormalizeBrand(email.Branding);
        var subject = $"Bienvenido a {brand.DisplayName}";
        var textBody = $"""
            Hola {email.RecipientName},

            Tu acceso a {brand.DisplayName} esta listo.

            Entra aqui:
            {email.LoginUrl}

            Si no esperabas este correo, puedes ignorarlo.
            """;
        var bodyHtml = $"""
            <h1>Bienvenido</h1>
            <p>Hola {Html(email.RecipientName)},</p>
            <p>Tu acceso a <strong>{Html(brand.DisplayName)}</strong> esta listo.</p>
            {Button(email.LoginUrl, "Entrar", brand)}
            {FallbackLink(email.LoginUrl)}
            """;

        return new RenderedEmailTemplate(
            EmailTemplateKind.Welcome,
            email.To,
            subject,
            textBody,
            Layout("Bienvenido", bodyHtml, brand));
    }

    public RenderedEmailTemplate RenderPasswordReset(PasswordResetEmail email)
    {
        var brand = NormalizeBrand(email.Branding);
        var subject = $"Restablece tu contrasena de {brand.DisplayName}";
        var expiration = email.ExpiresAt.ToLocalTime().ToString("g");
        var textBody = $"""
            Hola {email.RecipientName},

            Recibimos una solicitud para restablecer la contrasena de tu cuenta {email.AccountType}.

            Usa este link antes de {expiration}:
            {email.ResetUrl}

            Si no solicitaste este cambio, ignora este correo.
            """;
        var bodyHtml = $"""
            <h1>Restablecer contrasena</h1>
            <p>Hola {Html(email.RecipientName)},</p>
            <p>Recibimos una solicitud para restablecer la contrasena de tu cuenta <strong>{Html(email.AccountType)}</strong>.</p>
            <p>Este link vence: <strong>{Html(expiration)}</strong>.</p>
            {Button(email.ResetUrl, "Restablecer contrasena", brand)}
            {FallbackLink(email.ResetUrl)}
            """;

        return new RenderedEmailTemplate(
            EmailTemplateKind.PasswordReset,
            email.To,
            subject,
            textBody,
            Layout("Restablecer contrasena", bodyHtml, brand));
    }

    public RenderedEmailTemplate RenderInternalAlert(InternalAlertEmail email)
    {
        var brand = new EmailBranding(DefaultBrandName);
        var subject = $"[{email.Severity}] {email.AlertTitle}";
        var textBody = $"""
            {email.AlertTitle}

            Severidad: {email.Severity}

            {email.AlertSummary}
            {email.ActionUrl}
            """;
        var action = string.IsNullOrWhiteSpace(email.ActionUrl)
            ? string.Empty
            : Button(email.ActionUrl, "Abrir diagnostico", brand);
        var bodyHtml = $"""
            <h1>{Html(email.AlertTitle)}</h1>
            <p><strong>Severidad:</strong> {Html(email.Severity)}</p>
            <p>{Html(email.AlertSummary)}</p>
            {action}
            """;

        return new RenderedEmailTemplate(
            EmailTemplateKind.InternalAlert,
            email.To,
            subject,
            textBody,
            Layout("Alerta interna", bodyHtml, brand));
    }

    private static string Layout(string title, string bodyHtml, EmailBranding branding)
    {
        var brand = NormalizeBrand(branding);
        var primaryColor = NormalizeColor(brand.PrimaryColor);
        var logoHtml = BuildLogoHtml(brand);
        var programName = Html(brand.ProgramName ?? brand.DisplayName);

        return $"""
            <!doctype html>
            <html lang="es">
            <head>
              <meta charset="utf-8">
              <title>{Html(title)}</title>
            </head>
            <body style="margin:0;padding:24px;background:#f4f4f4;font-family:Arial,sans-serif;color:#222;">
              <table role="presentation" width="100%" cellspacing="0" cellpadding="0">
                <tr>
                  <td align="center">
                    <table role="presentation" width="600" cellspacing="0" cellpadding="0" style="max-width:600px;background:#ffffff;border-radius:8px;padding:24px;">
                      <tr>
                        <td>
                          {logoHtml}
                          <p style="margin:0 0 8px;color:{primaryColor};font-weight:bold;text-transform:uppercase;font-size:13px;">{programName}</p>
                          {bodyHtml}
                        </td>
                      </tr>
                    </table>
                  </td>
                </tr>
              </table>
            </body>
            </html>
            """;
    }

    private static EmailBranding NormalizeBrand(EmailBranding? branding)
    {
        if (branding is null || string.IsNullOrWhiteSpace(branding.DisplayName))
        {
            return new EmailBranding(DefaultBrandName, PrimaryColor: DefaultPrimaryColor);
        }

        return branding;
    }

    private static string Button(string url, string text, EmailBranding branding)
    {
        return $"""
            <p style="margin:28px 0;">
              <a href="{SafeHref(url)}" style="background:{NormalizeColor(branding.PrimaryColor)};color:#ffffff;text-decoration:none;padding:12px 18px;border-radius:6px;display:inline-block;">{Html(text)}</a>
            </p>
            """;
    }

    private static string FallbackLink(string url)
    {
        if (!IsSafePublicUrl(url))
        {
            return """<p style="color:#666;font-size:14px;">El link no esta disponible en este correo.</p>""";
        }

        return $"""
            <p style="color:#666;font-size:14px;">Si el boton no funciona, copia y pega este link en tu navegador:</p>
            <p style="word-break:break-all;color:#444;font-size:14px;">{Html(url)}</p>
            """;
    }

    private static string BuildLogoHtml(EmailBranding brand)
    {
        if (!IsSafePublicUrl(brand.LogoUrl))
        {
            return string.Empty;
        }

        return $"""<img src="{SafeHref(brand.LogoUrl!)}" alt="{Html(brand.DisplayName)}" width="72" style="display:block;margin:0 0 16px;max-width:72px;height:auto;" />""";
    }

    private static string NormalizeColor(string? value)
    {
        return IsHexColor(value) ? value! : DefaultPrimaryColor;
    }

    private static bool IsHexColor(string? value)
    {
        return value is { Length: 7 } &&
            value[0] == '#' &&
            value.Skip(1).All(character =>
                (character >= '0' && character <= '9') ||
                (character >= 'a' && character <= 'f') ||
                (character >= 'A' && character <= 'F'));
    }

    private static string SafeHref(string value)
    {
        return IsSafePublicUrl(value) ? Html(value) : "#";
    }

    private static bool IsSafePublicUrl(string? value)
    {
        return Uri.TryCreate(value, UriKind.Absolute, out var uri) &&
            (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
    }

    private static string Html(string value)
    {
        return WebUtility.HtmlEncode(value);
    }
}
