using System.Net;
using DigitalCards.Application.Abstractions;
using DigitalCards.Application.Models;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;

namespace DigitalCards.Infrastructure.Email;

public sealed class SmtpEmailSender : IEmailSender
{
    private readonly ILogger<SmtpEmailSender> _logger;
    private readonly SmtpEmailOptions _options;

    public SmtpEmailSender(
        IOptions<SmtpEmailOptions> options,
        ILogger<SmtpEmailSender> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task SendWalletEnrollmentAsync(
        WalletEnrollmentEmail email,
        CancellationToken cancellationToken = default)
    {
        ValidateOptions();
        var fromAddress = _options.FromAddress!;
        var host = _options.Host!;
        var userName = _options.UserName!;
        var password = _options.Password!;

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_options.FromName, fromAddress));
        message.To.Add(MailboxAddress.Parse(email.To));
        message.Subject = $"Tu tarjeta digital de {email.BusinessName} esta lista";

        var bodyBuilder = new BodyBuilder
        {
            TextBody = BuildTextBody(email),
            HtmlBody = BuildHtmlBody(email)
        };
        message.Body = bodyBuilder.ToMessageBody();

        using var client = new SmtpClient();
        await client.ConnectAsync(
            host,
            _options.Port,
            GetSecureSocketOptions(),
            cancellationToken);

        await client.AuthenticateAsync(userName, password, cancellationToken);
        await client.SendAsync(message, cancellationToken);
        await client.DisconnectAsync(quit: true, cancellationToken);

        _logger.LogInformation("Sent wallet enrollment email to {Recipient}.", MaskEmail(email.To));
    }

    private void ValidateOptions()
    {
        Require(_options.Host, "DigitalCards:Email:Host");
        Require(_options.FromAddress, "DigitalCards:Email:FromAddress");
        Require(_options.UserName, "DigitalCards:Email:UserName");
        Require(_options.Password, "DigitalCards:Email:Password");

        if (_options.Port <= 0)
        {
            throw new InvalidOperationException("DigitalCards:Email:Port must be greater than zero.");
        }

        _ = GetSecureSocketOptions();
    }

    private SecureSocketOptions GetSecureSocketOptions()
    {
        if (Enum.TryParse<SecureSocketOptions>(_options.SecureSocket, ignoreCase: true, out var secureSocketOptions))
        {
            return secureSocketOptions;
        }

        throw new InvalidOperationException("DigitalCards:Email:SecureSocket must be a valid MailKit SecureSocketOptions value.");
    }

    private static void Require(string? value, string key)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"{key} is required when SMTP email is enabled.");
        }
    }

    private static string BuildTextBody(WalletEnrollmentEmail email)
    {
        return $"""
            Hola {email.ClientName},

            Tu tarjeta digital de {email.BusinessName} esta lista.

            Abre este link para elegir Apple Wallet o Google Wallet:
            {email.EnrollmentUrl}

            Gracias por usar DigitalCards.
            """;
    }

    private static string MaskEmail(string email)
    {
        var normalized = email.Trim().ToLowerInvariant();
        var atIndex = normalized.IndexOf('@');
        return atIndex <= 1 ? "***" : string.Concat(normalized[0], "***", normalized[atIndex..]);
    }

    private static string BuildHtmlBody(WalletEnrollmentEmail email)
    {
        var clientName = WebUtility.HtmlEncode(email.ClientName);
        var businessName = WebUtility.HtmlEncode(email.BusinessName);
        var enrollmentUrl = WebUtility.HtmlEncode(email.EnrollmentUrl);
        var programName = WebUtility.HtmlEncode(email.ProgramName ?? "Tarjeta digital");
        var primaryColor = WebUtility.HtmlEncode(email.PrimaryColor ?? "#111827");
        var logoUrl = string.IsNullOrWhiteSpace(email.BusinessLogoUrl)
            ? string.Empty
            : $"""<img src="{WebUtility.HtmlEncode(email.BusinessLogoUrl)}" alt="{businessName}" width="72" style="display:block;margin:0 0 16px;max-width:72px;height:auto;" />""";

        return $"""
            <!doctype html>
            <html lang="es">
            <body style="margin:0;padding:24px;background:#f4f4f4;font-family:Arial,sans-serif;color:#222;">
              <table role="presentation" width="100%" cellspacing="0" cellpadding="0">
                <tr>
                  <td align="center">
                    <table role="presentation" width="600" cellspacing="0" cellpadding="0" style="max-width:600px;background:#ffffff;border-radius:8px;padding:24px;">
                      <tr>
                        <td>
                          {logoUrl}
                          <p style="margin:0 0 8px;color:{primaryColor};font-weight:bold;text-transform:uppercase;font-size:13px;">{programName}</p>
                          <h1 style="margin:0 0 16px;font-size:24px;">Tu tarjeta digital esta lista</h1>
                          <p>Hola {clientName},</p>
                          <p>Tu tarjeta digital de <strong>{businessName}</strong> esta lista para agregarse a tu billetera digital.</p>
                          <p>Elige Apple Wallet o Google Wallet desde el siguiente link:</p>
                          <p style="margin:28px 0;">
                            <a href="{enrollmentUrl}" style="background:{primaryColor};color:#ffffff;text-decoration:none;padding:12px 18px;border-radius:6px;display:inline-block;">Abrir tarjeta digital</a>
                          </p>
                          <p style="color:#666;font-size:14px;">Si el boton no funciona, copia y pega este link en tu navegador:</p>
                          <p style="word-break:break-all;color:#444;font-size:14px;">{enrollmentUrl}</p>
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
}
