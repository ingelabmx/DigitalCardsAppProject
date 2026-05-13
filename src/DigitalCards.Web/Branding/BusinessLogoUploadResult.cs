namespace DigitalCards.Web.Branding;

public sealed record BusinessLogoUploadResult(
    string? PublicPath,
    string? ErrorMessage)
{
    public bool Succeeded => PublicPath is not null;
}

