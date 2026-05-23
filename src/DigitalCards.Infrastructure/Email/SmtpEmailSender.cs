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
    private readonly IEmailTemplateRenderer _templates;

    public SmtpEmailSender(
        IOptions<SmtpEmailOptions> options,
        IEmailTemplateRenderer templates,
        ILogger<SmtpEmailSender> logger)
    {
        _options = options.Value;
        _templates = templates;
        _logger = logger;
    }

    public async Task SendWalletEnrollmentAsync(
        WalletEnrollmentEmail email,
        CancellationToken cancellationToken = default)
    {
        var rendered = _templates.RenderWalletEnrollment(email);
        await SendRenderedAsync(rendered, cancellationToken);

        _logger.LogInformation("Sent wallet enrollment email to {Recipient}.", MaskEmail(email.To));
    }

    public async Task SendPasswordResetAsync(
        PasswordResetEmail email,
        CancellationToken cancellationToken = default)
    {
        var rendered = _templates.RenderPasswordReset(email);
        await SendRenderedAsync(rendered, cancellationToken);

        _logger.LogInformation("Sent password reset email to {Recipient}.", MaskEmail(email.To));
    }

    public async Task SendLandingContactAsync(
        LandingContactEmail email,
        CancellationToken cancellationToken = default)
    {
        var rendered = _templates.RenderLandingContact(email);
        await SendRenderedAsync(rendered, cancellationToken);

        _logger.LogInformation("Sent landing contact email to {Recipient}.", MaskEmail(email.To));
    }

    public async Task SendPasswordChangedAsync(
        PasswordChangedEmail email,
        CancellationToken cancellationToken = default)
    {
        var rendered = _templates.RenderPasswordChanged(email);
        await SendRenderedAsync(rendered, cancellationToken);

        _logger.LogInformation("Sent password changed email to {Recipient}.", MaskEmail(email.To));
    }

    private async Task SendRenderedAsync(
        RenderedEmailTemplate rendered,
        CancellationToken cancellationToken)
    {
        ValidateOptions();
        var fromAddress = _options.FromAddress!;
        var host = _options.Host!;
        var userName = _options.UserName!;
        var password = _options.Password!;

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_options.FromName, fromAddress));
        message.To.Add(MailboxAddress.Parse(rendered.To));
        message.Subject = rendered.Subject;

        var bodyBuilder = new BodyBuilder
        {
            TextBody = rendered.TextBody,
            HtmlBody = rendered.HtmlBody
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

    private static string MaskEmail(string email)
    {
        var normalized = email.Trim().ToLowerInvariant();
        var atIndex = normalized.IndexOf('@');
        return atIndex <= 1 ? "***" : string.Concat(normalized[0], "***", normalized[atIndex..]);
    }
}
