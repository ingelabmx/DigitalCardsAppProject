using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DigitalCards.Web.Pages.Wallet;

public sealed class AppleModel : PageModel
{
    public string Token { get; private set; } = string.Empty;

    public void OnGet(string token)
    {
        Token = token;
    }
}

