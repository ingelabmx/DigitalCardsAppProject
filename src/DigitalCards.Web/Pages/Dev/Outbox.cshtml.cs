using DigitalCards.Application.Abstractions;
using DigitalCards.Application.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DigitalCards.Web.Pages.Dev;

public sealed class OutboxModel : PageModel
{
    private readonly IWalletEmailOutbox _outbox;
    private readonly IPasswordResetEmailOutbox _passwordResetOutbox;

    public OutboxModel(
        IWalletEmailOutbox outbox,
        IPasswordResetEmailOutbox passwordResetOutbox)
    {
        _outbox = outbox;
        _passwordResetOutbox = passwordResetOutbox;
    }

    public IReadOnlyList<WalletEnrollmentEmail> Messages { get; private set; } = [];

    public IReadOnlyList<PasswordResetEmail> PasswordResetMessages { get; private set; } = [];

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        Messages = await _outbox.ListAsync(cancellationToken);
        PasswordResetMessages = await _passwordResetOutbox.ListPasswordResetsAsync(cancellationToken);
    }
}
