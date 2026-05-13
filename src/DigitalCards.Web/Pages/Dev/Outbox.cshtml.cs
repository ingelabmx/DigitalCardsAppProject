using DigitalCards.Application.Abstractions;
using DigitalCards.Application.Models;
using DigitalCards.Web.Diagnostics;
using DigitalCards.Web.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DigitalCards.Web.Pages.Dev;

public sealed class OutboxModel : PageModel
{
    private readonly IWalletEmailOutbox _outbox;
    private readonly IPasswordResetEmailOutbox _passwordResetOutbox;
    private readonly IWebHostEnvironment _environment;
    private readonly IConfiguration _configuration;

    public OutboxModel(
        IWalletEmailOutbox outbox,
        IPasswordResetEmailOutbox passwordResetOutbox,
        IWebHostEnvironment environment,
        IConfiguration configuration)
    {
        _outbox = outbox;
        _passwordResetOutbox = passwordResetOutbox;
        _environment = environment;
        _configuration = configuration;
    }

    public IReadOnlyList<WalletEnrollmentEmail> Messages { get; private set; } = [];

    public IReadOnlyList<PasswordResetEmail> PasswordResetMessages { get; private set; } = [];

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        if (!DevToolAccess.IsDevOutboxEnabled(_environment, _configuration))
        {
            return NotFound();
        }

        if (!DevToolAccess.CanAccessDevOutbox(_environment, _configuration, User))
        {
            return Challenge(AdminAuth.Scheme);
        }

        Messages = await _outbox.ListAsync(cancellationToken);
        PasswordResetMessages = await _passwordResetOutbox.ListPasswordResetsAsync(cancellationToken);
        return Page();
    }
}
