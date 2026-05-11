using Microsoft.AspNetCore.Http;

namespace DigitalCards.Web;

public static class EnrollmentBaseUrlResolver
{
    public static string Resolve(string? publicBaseUrl, string requestScheme, HostString requestHost)
    {
        if (!string.IsNullOrWhiteSpace(publicBaseUrl))
        {
            if (!Uri.TryCreate(publicBaseUrl.Trim(), UriKind.Absolute, out var uri) ||
                (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            {
                throw new InvalidOperationException("DigitalCards:PublicBaseUrl must be an absolute HTTP(S) URL.");
            }

            return publicBaseUrl.Trim().TrimEnd('/');
        }

        return $"{requestScheme}://{requestHost}".TrimEnd('/');
    }
}
