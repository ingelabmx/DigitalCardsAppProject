using DigitalCards.Application.Services;

namespace DigitalCards.Web;

public static class AppleWalletPassDownloadEndpoints
{
    public static IEndpointRouteBuilder MapAppleWalletPassDownloads(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapMethods(
            "/Wallet/Apple/Download/{token}.pkpass",
            [HttpMethods.Get, HttpMethods.Head],
            async (
                string token,
                DigitalCardsAppService appService,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                try
                {
                    var passFile = await appService.DownloadAppleWalletPassAsync(token, cancellationToken);
                    if (passFile is null)
                    {
                        return Results.NotFound();
                    }

                    SetNoStore(httpContext.Response);
                    return Results.File(
                        passFile.Content,
                        passFile.ContentType,
                        passFile.FileName,
                        lastModified: passFile.LastModified);
                }
                catch (InvalidOperationException)
                {
                    return Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
                }
            });

        return endpoints;
    }

    private static void SetNoStore(HttpResponse response)
    {
        response.Headers.CacheControl = "no-store, no-cache, must-revalidate";
        response.Headers.Pragma = "no-cache";
        response.Headers.Expires = "0";
    }
}
