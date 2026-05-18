namespace DigitalCards.Web.Landing;

public static class LandingHost
{
    public static bool IsLandingHost(HttpContext context, LandingOptions options)
    {
        return Uri.TryCreate(options.SiteUrl, UriKind.Absolute, out var siteUri) &&
            string.Equals(context.Request.Host.Host, siteUri.Host, StringComparison.OrdinalIgnoreCase);
    }

    public static bool ShouldRedirectToApp(HttpContext context, LandingOptions options)
    {
        if (!IsLandingHost(context, options))
        {
            return false;
        }

        var path = context.Request.Path;
        return !IsLandingAllowedPath(path);
    }

    private static bool IsLandingAllowedPath(PathString path)
    {
        return path == "/" ||
            path.StartsWithSegments("/terminos", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWithSegments("/privacidad", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWithSegments("/robots.txt", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWithSegments("/sitemap.xml", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWithSegments("/favicon.ico", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWithSegments("/css", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWithSegments("/js", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWithSegments("/img", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWithSegments("/landing", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWithSegments("/lib", StringComparison.OrdinalIgnoreCase);
    }
}
