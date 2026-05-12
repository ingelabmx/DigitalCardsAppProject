using DigitalCards.Application.Models;
using DigitalCards.Application.Services;
using DigitalCards.Web.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DigitalCards.Web.Pages.Admin;

[Authorize(Policy = AdminAuth.Policy)]
public sealed class AdminUsersModel : PageModel
{
    private readonly AdminAppService _adminApp;
    private readonly ILogger<AdminUsersModel> _logger;

    public AdminUsersModel(AdminAppService adminApp, ILogger<AdminUsersModel> logger)
    {
        _adminApp = adminApp;
        _logger = logger;
    }

    public IReadOnlyList<AdminUserListItemDto> AdminUsers { get; private set; } = [];

    public string? StatusMessage { get; private set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        await LoadAsync(cancellationToken);
    }

    public async Task<IActionResult> OnPostResetPasswordAsync(
        Guid targetAdminUserId,
        string newPassword,
        string confirmPassword,
        CancellationToken cancellationToken)
    {
        if (targetAdminUserId == Guid.Empty)
        {
            ModelState.AddModelError(string.Empty, "El admin no existe.");
        }

        if (!string.Equals(newPassword, confirmPassword, StringComparison.Ordinal))
        {
            ModelState.AddModelError(string.Empty, "Las contrasenas no coinciden.");
        }

        if (!ModelState.IsValid)
        {
            await LoadAsync(cancellationToken);
            return Page();
        }

        var result = await _adminApp.ResetAdminPasswordAsync(
            new ResetAdminPasswordCommand(
                targetAdminUserId,
                AdminAuth.GetAdminUserId(User),
                newPassword),
            cancellationToken);

        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "No se pudo actualizar la contrasena del admin.");
            await LoadAsync(cancellationToken);
            return Page();
        }

        _logger.LogInformation(
            "Admin {AdminUserId} reset password for admin user {TargetAdminUserId}.",
            AdminAuth.GetAdminUserId(User),
            result.Admin!.Id);
        StatusMessage = $"Contrasena de admin actualizada para {result.Admin.UserName}.";
        await LoadAsync(cancellationToken);
        return Page();
    }

    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        AdminUsers = await _adminApp.ListAdminUsersAsync(cancellationToken);
    }
}
