namespace DigitalCards.Web.Pilot;

public sealed record PilotAccessResult(bool IsAllowed, string? Message)
{
    public static PilotAccessResult Allowed { get; } = new(true, null);

    public static PilotAccessResult Blocked(string message)
    {
        return new PilotAccessResult(false, message);
    }
}
