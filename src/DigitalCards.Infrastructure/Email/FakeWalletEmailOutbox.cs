using DigitalCards.Application.Abstractions;
using DigitalCards.Application.Models;

namespace DigitalCards.Infrastructure.Email;

public sealed class FakeWalletEmailOutbox : IEmailSender, IWalletEmailOutbox
{
    private readonly List<WalletEnrollmentEmail> _messages = [];
    private readonly object _sync = new();

    public Task SendWalletEnrollmentAsync(WalletEnrollmentEmail email, CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            _messages.Insert(0, email);
        }

        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<WalletEnrollmentEmail>> ListAsync(CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            return Task.FromResult<IReadOnlyList<WalletEnrollmentEmail>>(_messages.ToArray());
        }
    }
}

