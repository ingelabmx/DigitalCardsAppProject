namespace DigitalCards.Application.Models;

public sealed record SignupCommand(
    string BusinessName,
    string BusinessEmail,
    string Password,
    string PlanKey);

public sealed record SignupResult(Guid? BusinessId, string? ErrorMessage)
{
    public bool Succeeded => BusinessId.HasValue;
}

public sealed record AbandonedSignupDto(
    Guid BusinessId,
    string BusinessName,
    string BusinessEmail,
    string? PlanKey,
    DateTimeOffset CreatedAt);
