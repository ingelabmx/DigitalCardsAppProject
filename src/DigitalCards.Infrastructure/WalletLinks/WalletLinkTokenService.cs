using System.Security.Cryptography;
using System.Text;
using DigitalCards.Application.Abstractions;
using DigitalCards.Application.Models;
using Microsoft.Extensions.Options;

namespace DigitalCards.Infrastructure.WalletLinks;

public sealed class WalletLinkTokenService : IWalletLinkTokenService
{
    private const int TokenByteLength = 32;
    private const int TokenSuffixLength = 8;
    private const int MaxAttempts = 3;
    private readonly IClock _clock;
    private readonly IWalletLinkTokenRepository _tokens;
    private readonly WalletLinkOptions _options;

    public WalletLinkTokenService(
        IWalletLinkTokenRepository tokens,
        IClock clock,
        IOptions<WalletLinkOptions> options)
    {
        _tokens = tokens;
        _clock = clock;
        _options = options.Value;
    }

    public bool AllowLegacyCardIdTokens => _options.AllowLegacyCardIdTokens;

    public async Task<string> CreateTokenAsync(
        Guid cardId,
        string purpose,
        CancellationToken cancellationToken = default)
    {
        for (var attempt = 0; attempt < MaxAttempts; attempt++)
        {
            var token = CreateOpaqueToken();
            var hash = HashToken(token);
            var existing = await _tokens.FindActiveByTokenHashAsync(hash, purpose, cancellationToken);
            if (existing is not null)
            {
                continue;
            }

            await _tokens.AddAsync(
                new WalletLinkTokenRecord(
                    cardId,
                    purpose,
                    hash,
                    Suffix(token),
                    _clock.UtcNow,
                    LastUsedAt: null,
                    RevokedAt: null),
                cancellationToken);

            return token;
        }

        throw new InvalidOperationException("Could not create a unique wallet link token.");
    }

    public async Task<Guid?> ResolveCardIdAsync(
        string token,
        string purpose,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return null;
        }

        var hash = HashToken(token.Trim());
        var record = await _tokens.FindActiveByTokenHashAsync(hash, purpose, cancellationToken);
        if (record is null)
        {
            return null;
        }

        await _tokens.MarkUsedAsync(hash, purpose, _clock.UtcNow, cancellationToken);
        return record.CardId;
    }

    public async Task RevokeActiveByCardIdAsync(
        Guid cardId,
        string purpose,
        CancellationToken cancellationToken = default)
    {
        await _tokens.RevokeActiveByCardIdAsync(cardId, purpose, _clock.UtcNow, cancellationToken);
    }

    private static string CreateOpaqueToken()
    {
        Span<byte> bytes = stackalloc byte[TokenByteLength];
        RandomNumberGenerator.Fill(bytes);
        return Base64UrlEncode(bytes);
    }

    private static string HashToken(string token)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static string Suffix(string token)
    {
        return token.Length <= TokenSuffixLength ? token : token[^TokenSuffixLength..];
    }

    private static string Base64UrlEncode(ReadOnlySpan<byte> bytes)
    {
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }
}
