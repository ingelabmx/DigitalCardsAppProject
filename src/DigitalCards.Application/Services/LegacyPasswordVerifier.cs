using System.Security.Cryptography;
using System.Text;

namespace DigitalCards.Application.Services;

internal static class LegacyPasswordVerifier
{
    public static bool Matches(string storedPassword, string candidatePassword)
    {
        if (string.Equals(storedPassword, candidatePassword, StringComparison.Ordinal))
        {
            return true;
        }

        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(candidatePassword))).ToLowerInvariant();

        return string.Equals(storedPassword, hash, StringComparison.Ordinal)
            || hash.StartsWith(storedPassword, StringComparison.Ordinal);
    }
}
