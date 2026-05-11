namespace DigitalCards.Infrastructure.Email;

public sealed class SmtpEmailOptions
{
    public const string SectionName = $"{DigitalCardsInfrastructureOptions.SectionName}:Email";

    public string? Provider { get; init; }

    public string FromName { get; init; } = "DigitalCards";

    public string? FromAddress { get; init; }

    public string? Host { get; init; }

    public int Port { get; init; } = 587;

    public string SecureSocket { get; init; } = "StartTls";

    public string? UserName { get; init; }

    public string? Password { get; init; }

    public string? SmokeRecipient { get; init; }
}
