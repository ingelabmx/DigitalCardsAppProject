using DigitalCards.Application.Abstractions;
using DigitalCards.Application.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DigitalCards.Web.Pages.Dev;

public sealed class OutboxModel : PageModel
{
    private readonly IWalletEmailOutbox _outbox;

    public OutboxModel(IWalletEmailOutbox outbox)
    {
        _outbox = outbox;
    }

    public IReadOnlyList<WalletEnrollmentEmail> Messages { get; private set; } = [];

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        Messages = await _outbox.ListAsync(cancellationToken);
    }
}

