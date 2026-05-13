using System.Security.Cryptography;
using System.Text;
using DigitalCards.Application.Abstractions;
using DigitalCards.Application.Models;

namespace DigitalCards.Infrastructure.WalletLinks;

public sealed class BusinessEnrollmentLinkService : IBusinessEnrollmentLinkService
{
    private const int TokenByteLength = 32;
    private const int TokenSuffixLength = 8;
    private const int MaxAttempts = 3;
    private readonly IBusinessEnrollmentLinkRepository _links;
    private readonly IClock _clock;

    public BusinessEnrollmentLinkService(
        IBusinessEnrollmentLinkRepository links,
        IClock clock)
    {
        _links = links;
        _clock = clock;
    }

    public async Task<string> CreateOrReplaceTokenAsync(
        Guid businessId,
        CancellationToken cancellationToken = default)
    {
        await _links.RevokeActiveByBusinessIdAsync(businessId, _clock.UtcNow, cancellationToken);

        for (var attempt = 0; attempt < MaxAttempts; attempt++)
        {
            var token = CreateOpaqueToken();
            var hash = HashToken(token);
            var existing = await _links.FindActiveByTokenHashAsync(hash, cancellationToken);
            if (existing is not null)
            {
                continue;
            }

            await _links.AddAsync(
                new BusinessEnrollmentLinkRecord(
                    businessId,
                    hash,
                    Suffix(token),
                    _clock.UtcNow,
                    LastUsedAt: null,
                    RevokedAt: null),
                cancellationToken);

            return token;
        }

        throw new InvalidOperationException("Could not create a unique business enrollment token.");
    }

    public async Task<Guid?> ResolveBusinessIdAsync(string token, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return null;
        }

        var hash = HashToken(token.Trim());
        var record = await _links.FindActiveByTokenHashAsync(hash, cancellationToken);
        if (record is null)
        {
            return null;
        }

        await _links.MarkUsedAsync(hash, _clock.UtcNow, cancellationToken);
        return record.BusinessId;
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
