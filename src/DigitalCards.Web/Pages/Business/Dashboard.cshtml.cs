using DigitalCards.Application.Abstractions;
using DigitalCards.Application.Models;
using DigitalCards.Application.Services;
using DigitalCards.Web.Pilot;
using DigitalCards.Web.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DigitalCards.Web.Pages.Business;

[Authorize(Policy = BusinessAuth.Policy)]
public sealed class DashboardModel : PageModel
{
    private readonly DigitalCardsAppService _appService;
    private readonly IBusinessEnrollmentLinkService _businessEnrollmentLinks;
    private readonly PilotAccessService _pilotAccess;
    private readonly IConfiguration _configuration;

    public DashboardModel(
        DigitalCardsAppService appService,
        IBusinessEnrollmentLinkService businessEnrollmentLinks,
        PilotAccessService pilotAccess,
        IConfiguration configuration)
    {
        _appService = appService;
        _businessEnrollmentLinks = businessEnrollmentLinks;
        _pilotAccess = pilotAccess;
        _configuration = configuration;
    }

    public string BusinessName { get; private set; } = string.Empty;

    public BusinessDashboardDto? Dashboard { get; private set; }

    public string? PilotBlockMessage { get; private set; }

    public bool IsPilotBlocked => PilotBlockMessage is not null;

    public string? GeneratedEnrollmentUrl { get; private set; }

    public string? GeneratedEnrollmentQrSvg { get; private set; }

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        return await LoadAsync(cancellationToken);
    }

    public async Task<IActionResult> OnPostGenerateEnrollmentLinkAsync(CancellationToken cancellationToken)
    {
        var result = await LoadAsync(cancellationToken);
        if (result is not PageResult || IsPilotBlocked || Dashboard is null)
        {
            return result;
        }

        var token = await _businessEnrollmentLinks.CreateOrReplaceTokenAsync(
            Dashboard.Business.Id,
            cancellationToken);
        GeneratedEnrollmentUrl = $"{GetBaseUrl()}/Enroll/{token}";
        GeneratedEnrollmentQrSvg = EnrollmentQrCodeRenderer.RenderSvg(GeneratedEnrollmentUrl);
        return Page();
    }

    private async Task<IActionResult> LoadAsync(CancellationToken cancellationToken)
    {
        var businessId = BusinessAuth.GetBusinessId(User);
        Dashboard = await _appService.GetBusinessDashboardAsync(businessId, cancellationToken);
        if (Dashboard is null)
        {
            return RedirectToPage("/Business/Logout");
        }

        BusinessName = Dashboard.Business.Name;
        ViewData["BusinessShellName"] = BusinessName;
        var pilotAccess = await _pilotAccess.CheckBusinessAsync(
            Dashboard.Business.Id,
            Dashboard.Business.Email,
            cancellationToken);
        if (!pilotAccess.IsAllowed)
        {
            PilotBlockMessage = pilotAccess.Message;
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
