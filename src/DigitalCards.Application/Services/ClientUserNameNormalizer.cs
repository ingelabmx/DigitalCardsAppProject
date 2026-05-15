namespace DigitalCards.Application.Services;

public static class ClientUserNameNormalizer
{
    public static string NormalizeUserName(string userName)
    {
        return (userName ?? string.Empty).Trim().ToLowerInvariant();
    }

    public static string NormalizeUserNameOrEmail(string userNameOrEmail)
    {
        var value = (userNameOrEmail ?? string.Empty).Trim();
        return value.Contains('@', StringComparison.Ordinal)
            ? value.ToLowerInvariant()
            : NormalizeUserName(value);
    }

    public static bool IsValidUserName(string userName)
    {
        var normalized = NormalizeUserName(userName);
        return normalized.Length > 0 && normalized.All(IsAsciiLetterOrDigit);
    }

    private static bool IsAsciiLetterOrDigit(char value)
    {
        return (value >= 'a' && value <= 'z') ||
               (value >= 'A' && value <= 'Z') ||
               (value >= '0' && value <= '9');
    }
}
