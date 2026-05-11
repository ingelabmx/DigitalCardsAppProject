using DigitalCards.Application.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DigitalCards.Web.Pages.Wallet.Apple;

public sealed class DownloadModel : PageModel
{
    private readonly DigitalCardsAppService _appService;

    public DownloadModel(DigitalCardsAppService appService)
    {
        _appService = appService;
    }

    public async Task<IActionResult> OnGetAsync(string token, CancellationToken cancellationToken)
    {
        try
        {
            var passFile = await _appService.DownloadAppleWalletPassAsync(token, cancellationToken);
            if (passFile is null)
            {
                return NotFound();
            }

            Response.Headers.CacheControl = "no-store, no-cache, must-revalidate";
            Response.Headers.Pragma = "no-cache";
            Response.Headers.Expires = "0";

            return File(passFile.Content, passFile.ContentType, passFile.FileName);
        }
        catch (InvalidOperationException)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, "Apple Wallet pass generation is not available.");
        }
    }
}
