namespace DigitalCards.Application.Abstractions;

public interface IWalletLinkTokenService
{
    bool AllowLegacyCardIdTokens { get; }

    Task<string> CreateTokenAsync(
        Guid cardId,
        string purpose,
        CancellationToken cancellationToken = default);

    Task<Guid?> ResolveCardIdAsync(
        string token,
        string purpose,
        CancellationToken cancellationToken = default);

    Task RevokeActiveByCardIdAsync(
        Guid cardId,
        string purpose,
        CancellationToken cancellationToken = default);
}
