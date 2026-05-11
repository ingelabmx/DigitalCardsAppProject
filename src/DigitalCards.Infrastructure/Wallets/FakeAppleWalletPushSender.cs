using DigitalCards.Application.Abstractions;
using DigitalCards.Application.Models;

namespace DigitalCards.Infrastructure.Wallets;

public sealed class FakeAppleWalletPushSender : IAppleWalletPushSender
{
    public Task<AppleWalletPushResult> SendUpdateAsync(
        string pushToken,
        string passTypeIdentifier,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new AppleWalletPushResult(
            Accepted: true,
            ShouldDeleteDevice: false,
            Status: "Fake"));
    }
}
