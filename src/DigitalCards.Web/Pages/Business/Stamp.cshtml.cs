using System.ComponentModel.DataAnnotations;
using DigitalCards.Application.Models;
using DigitalCards.Application.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DigitalCards.Web.Pages.Business;

public sealed class StampModel : PageModel
{
    private readonly DigitalCardsAppService _appService;

    public StampModel(DigitalCardsAppService appService)
    {
        _appService = appService;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public LoyaltyCardDto? Result { get; private set; }

    public void OnGet(Guid businessId)
    {
        Input.BusinessId = businessId;
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        try
        {
            Result = await _appService.AddStampAsync(
                new AddStampCommand(Input.BusinessId, Input.UserNameOrEmail),
                cancellationToken);

            return Page();
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return Page();
        }
    }

    public sealed class InputModel
    {
        [Required]
        public Guid BusinessId { get; set; }

        [Display(Name = "Usuario o correo del cliente")]
        [Required]
        public string UserNameOrEmail { get; set; } = string.Empty;
    }
}

