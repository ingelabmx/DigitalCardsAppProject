using DigitalCards.Application.Models;
using DigitalCards.Application.Services;
using DigitalCards.Web.Security;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;

namespace DigitalCards.Web.Pages.Wallet;

[EnableRateLimiting(SecurityRateLimitPolicyNames.WalletPublic)]
public sealed class AppleModel : PageModel
{
    private readonly DigitalCardsAppService _appService;
    private readonly IConfiguration _configuration;

    public AppleModel(
        DigitalCardsAppService appService,
        IConfiguration configuration)
    {
        _appService = appService;
        _configuration = configuration;
    }

    public string Token { get; private set; } = string.Empty;

    public AppleWalletIssueResult? Result { get; private set; }

    public WalletLandingDto? Landing { get; private set; }

    public async Task<IActionResult> OnGetAsync(string token, CancellationToken cancellationToken)
    {
        Token = token;
        Landing = await _appService.GetWalletLandingAsync(token, cancellationToken);
        Result = await _appService.SelectAppleWalletAsync(token, GetBaseUrl(), cancellationToken);

        if (Result?.Status == AppleWalletIssueStatus.Ready &&
            !string.IsNullOrWhiteSpace(Result.DownloadUrl))
        {
            return Redirect(Result.DownloadUrl);
        }

        return Page();
    }

    private string GetBaseUrl()
    {
        return EnrollmentBaseUrlResolver.Resolve(
            _configuration["DigitalCards:PublicBaseUrl"],
            Request.Scheme,
            Request.Host);
    }
}
