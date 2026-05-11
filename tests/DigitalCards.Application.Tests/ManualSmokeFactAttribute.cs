namespace DigitalCards.Application.Tests;

public sealed class ManualSmokeFactAttribute : FactAttribute
{
    public ManualSmokeFactAttribute(string environmentVariable)
    {
        if (!string.Equals(Environment.GetEnvironmentVariable(environmentVariable), "1", StringComparison.Ordinal))
        {
            Skip = $"Set {environmentVariable}=1 to run this manual integration smoke test.";
        }
    }
}
