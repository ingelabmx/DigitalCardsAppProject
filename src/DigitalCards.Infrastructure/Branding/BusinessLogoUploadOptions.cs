namespace DigitalCards.Infrastructure.Branding;

public sealed class BusinessLogoUploadOptions
{
    public const string SectionName = $"{DigitalCardsInfrastructureOptions.SectionName}:Branding:LogoUploads";

    public string? Path { get; init; }

    public string RequestPath { get; init; } = "/uploads/business-logos";

    public long MaxBytes { get; init; } = 2 * 1024 * 1024;

    public string GetPhysicalRoot()
    {
        return string.IsNullOrWhiteSpace(Path)
            ? System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                ".digitalcards",
                "uploads",
                "business-logos")
            : System.IO.Path.GetFullPath(Environment.ExpandEnvironmentVariables(Path));
    }

    public string GetRequestPath()
    {
        var requestPath = string.IsNullOrWhiteSpace(RequestPath)
            ? "/uploads/business-logos"
            : RequestPath.Trim();

        return requestPath.StartsWith("/", StringComparison.Ordinal)
            ? requestPath.TrimEnd('/')
            : $"/{requestPath.TrimEnd('/')}";
    }
}

