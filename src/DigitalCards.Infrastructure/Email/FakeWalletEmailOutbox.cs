using DigitalCards.Application.Abstractions;
using DigitalCards.Application.Models;

namespace DigitalCards.Infrastructure.Email;

public sealed class FakeWalletEmailOutbox : IEmailSender, IWalletEmailOutbox, IPasswordResetEmailOutbox
{
    private readonly List<WalletEnrollmentEmail> _messages = [];
    private readonly List<PasswordResetEmail> _passwordResets = [];
    private readonly object _sync = new();

    public Task SendWalletEnrollmentAsync(WalletEnrollmentEmail email, CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            _messages.Insert(0, email);
        }

        return Task.CompletedTask;
    }

    public Task SendPasswordResetAsync(PasswordResetEmail email, CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            _passwordResets.Insert(0, email);
        }

        return Task.CompletedTask;
    }

    public Task SendLandingContactAsync(LandingContactEmail email, CancellationToken cancellationToken = default)
    {
        throw new InvalidOperationException("SMTP email is required for public landing contact forms.");
    }

    public Task<IReadOnlyList<WalletEnrollmentEmail>> ListAsync(CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            return Task.FromResult<IReadOnlyList<WalletEnrollmentEmail>>(_messages.ToArray());
        }
    }

    public Task<IReadOnlyList<PasswordResetEmail>> ListPasswordResetsAsync(
        CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            return Task.FromResult<IReadOnlyList<PasswordResetEmail>>(_passwordResets.ToArray());
        }
    }
}
