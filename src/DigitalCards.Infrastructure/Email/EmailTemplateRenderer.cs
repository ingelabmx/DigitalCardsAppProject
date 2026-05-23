using System.Net;
using DigitalCards.Application.Abstractions;
using DigitalCards.Application.Models;

namespace DigitalCards.Infrastructure.Email;

public sealed class EmailTemplateRenderer : IEmailTemplateRenderer
{
    private const string DefaultBrandName = "Puntelio";
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

            Gracias por usar Puntelio.
            """;
        var bodyHtml = $"""
            <h1>Tu tarjeta digital esta lista</h1>
            <p>Hola {Html(email.ClientName)},</p>
            <p>Tu tarjeta digital de <strong>{Html(email.BusinessName)}</strong> esta lista para agregarse a tu billetera digital.</p>
            <p>Elige Apple Wallet o Google Wallet desde el siguiente link:</p>
            {WalletStoreBadges(email.EnrollmentUrl, brand)}
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

    public RenderedEmailTemplate RenderLandingContact(LandingContactEmail email)
    {
        var brand = new EmailBranding(DefaultBrandName, PrimaryColor: DefaultPrimaryColor);
        var subject = "Nueva solicitud de videollamada - Puntelio";
        var textBody = $"""
            Nueva solicitud de videollamada

            Nombre:
            {email.Name}

            Nombre del negocio:
            {email.BusinessName}

            Correo:
            {email.Email}

            Telefono:
            {email.Phone}

            Tipo de solicitud:
            {email.RequestType}

            El usuario solicita agendar una videollamada por Google Meet.
            """;
        var bodyHtml = $"""
            <h1>Nueva solicitud de videollamada</h1>
            <p><strong>Nombre:</strong> {Html(email.Name)}</p>
            <p><strong>Nombre del negocio:</strong> {Html(email.BusinessName)}</p>
            <p><strong>Correo:</strong> {Html(email.Email)}</p>
            <p><strong>Telefono:</strong> {Html(email.Phone)}</p>
            <p><strong>Tipo de solicitud:</strong> {Html(email.RequestType)}</p>
            <p>El usuario solicita agendar una videollamada por Google Meet.</p>
            """;

        return new RenderedEmailTemplate(
            EmailTemplateKind.LandingContact,
            email.To,
            subject,
            textBody,
            Layout("Nueva solicitud de videollamada", bodyHtml, brand));
    }

    public RenderedEmailTemplate RenderPasswordChanged(PasswordChangedEmail email)
    {
        var brand = NormalizeBrand(email.Branding);
        var subject = $"Tu contrasena de {brand.DisplayName} fue cambiada";
        var changedAt = email.ChangedAt.ToLocalTime().ToString("g");
        var textBody = $"""
            Hola {email.RecipientName},

            Tu contrasena de {brand.DisplayName} ({email.AccountType}) fue cambiada exitosamente el {changedAt}.

            Si no realizaste este cambio, contacta a soporte de inmediato.
            """;
        var bodyHtml = $"""
            <h1>Contrasena cambiada</h1>
            <p>Hola {Html(email.RecipientName)},</p>
            <p>Tu contrasena de <strong>{Html(brand.DisplayName)}</strong> ({Html(email.AccountType)}) fue cambiada exitosamente.</p>
            <p><strong>Fecha:</strong> {Html(changedAt)}</p>
            <p>Si no realizaste este cambio, contacta a soporte de inmediato.</p>
            """;

        return new RenderedEmailTemplate(
            EmailTemplateKind.PasswordChanged,
            email.To,
            subject,
            textBody,
            Layout("Contrasena cambiada", bodyHtml, brand));
    }

    private static string Layout(string title, string bodyHtml, EmailBranding branding)
    {
        var brand = NormalizeBrand(branding);
        var primaryColor = NormalizeColor(brand.PrimaryColor);
        var brandHeaderHtml = BuildBrandHeaderHtml(brand, primaryColor);

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
                          {brandHeaderHtml}
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

    private static string WalletStoreBadges(string enrollmentUrl, EmailBranding branding)
    {
        if (!TryBuildWalletActionUrls(enrollmentUrl, out var appleUrl, out var googleUrl, out var origin))
        {
            return Button(enrollmentUrl, "Abrir tarjeta digital", branding);
        }

        var appleBadgeUrl = $"{origin}/img/add_to_apple_wallet.svg";
        var googleBadgeUrl = $"{origin}/img/add_to_google_wallet.svg";
        return $"""
            <table role="presentation" cellspacing="0" cellpadding="0" style="margin:24px 0;">
              <tr>
                <td style="padding:0 10px 10px 0;">
                  <a href="{SafeHref(appleUrl)}" style="display:inline-block;text-decoration:none;">
                    <img src="{SafeHref(appleBadgeUrl)}" alt="Add to Apple Wallet" width="199" height="55" style="display:block;border:0;width:199px;height:55px;object-fit:contain;" />
                  </a>
                </td>
                <td style="padding:0 0 10px 0;">
                  <a href="{SafeHref(googleUrl)}" style="display:inline-block;text-decoration:none;">
                    <img src="{SafeHref(googleBadgeUrl)}" alt="Add to Google Wallet" width="199" height="55" style="display:block;border:0;width:199px;height:55px;object-fit:contain;" />
                  </a>
                </td>
              </tr>
            </table>
            """;
    }

    private static bool TryBuildWalletActionUrls(
        string enrollmentUrl,
        out string appleUrl,
        out string googleUrl,
        out string origin)
    {
        appleUrl = string.Empty;
        googleUrl = string.Empty;
        origin = string.Empty;
        if (!Uri.TryCreate(enrollmentUrl, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            return false;
        }

        const string selectPrefix = "/Wallet/Select/";
        if (!uri.AbsolutePath.StartsWith(selectPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var token = uri.AbsolutePath[selectPrefix.Length..];
        if (string.IsNullOrWhiteSpace(token) || token.Contains('/', StringComparison.Ordinal))
        {
            return false;
        }

        origin = uri.GetLeftPart(UriPartial.Authority);
        appleUrl = $"{origin}/Wallet/Apple/{token}";
        googleUrl = $"{origin}/Wallet/Google/{token}";
        return true;
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

    private static string BuildBrandHeaderHtml(EmailBranding brand, string primaryColor)
    {
        var displayName = Html(brand.DisplayName);
        var programLine = string.IsNullOrWhiteSpace(brand.ProgramName)
            ? string.Empty
            : $"""<p style="margin:4px 0 0;color:{primaryColor};font-weight:bold;text-transform:uppercase;font-size:13px;">{Html(brand.ProgramName!)}</p>""";

        if (!IsSafePublicUrl(brand.LogoUrl))
        {
            return $"""
                <div style="margin:0 0 18px;">
                  <p style="margin:0;color:#111827;font-size:45px;font-weight:bold;line-height:1.2;">{displayName}</p>
                  {programLine}
                </div>
                """;
        }

        return $"""
            <table role="presentation" width="100%" cellspacing="0" cellpadding="0" style="margin:0 0 18px;">
              <tr>
                <td width="86" valign="middle" style="padding:0 14px 0 0;">
                  <img src="{SafeHref(brand.LogoUrl!)}" alt="{displayName}" width="72" style="display:block;border:0;max-width:72px;height:auto;" />
                </td>
                <td valign="middle" style="padding:0;">
                  <p style="margin:0;color:#111827;font-size:45px;font-weight:bold;line-height:1.2;">{displayName}</p>
                  {programLine}
                </td>
              </tr>
            </table>
            """;
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
