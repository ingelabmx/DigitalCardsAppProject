using DigitalCards.Application.Models;

namespace DigitalCards.Application.Abstractions;

public interface IAppleWalletPushSender
{
    Task<AppleWalletPushResult> SendUpdateAsync(
        string pushToken,
        string passTypeIdentifier,
        CancellationToken cancellationToken = default);
}
