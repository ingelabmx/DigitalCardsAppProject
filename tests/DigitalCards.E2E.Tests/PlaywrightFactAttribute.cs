using Xunit;

namespace DigitalCards.E2E.Tests;

public sealed class PlaywrightFactAttribute : FactAttribute
{
    public PlaywrightFactAttribute()
    {
        if (!string.Equals(Environment.GetEnvironmentVariable("RUN_PLAYWRIGHT"), "1", StringComparison.Ordinal))
        {
            Skip = "Set RUN_PLAYWRIGHT=1 and install Playwright browsers to run browser E2E tests.";
        }
    }
}

