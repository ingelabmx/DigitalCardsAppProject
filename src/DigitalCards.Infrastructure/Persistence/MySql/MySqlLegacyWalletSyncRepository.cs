using Dapper;
using DigitalCards.Application.Abstractions;
using DigitalCards.Application.Models;
using DigitalCards.Domain;

namespace DigitalCards.Infrastructure.Persistence.MySql;

public sealed class MySqlLegacyWalletSyncRepository : ILegacyWalletSyncRepository
{
    private readonly MySqlConnectionFactory _connectionFactory;

    public MySqlLegacyWalletSyncRepository(MySqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IReadOnlyList<LegacyWalletSyncCandidate>> ListCandidatesAsync(
        DateTimeOffset changedSince,
        int batchSize,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            select cc.CardID,
                   cc.CardIDGoogle,
                   cc.CreationDate,
                   cc.CheckQTY,
                   cc.LastCheck,
                   cc.UserID,
                   cc.BusinessID,
                   cc.HistoricCheckQTY,
                   uc.UserName,
                   uc.FirstName,
                   uc.Lastname,
                   uc.UserEmail,
                   b.BusinessName,
                   b.BusinessEmail,
                   b.BusinessPassword,
                   b.BusinessLogo,
                   exists (
                       select 1
                       from AppleWalletPass awp
                       inner join AppleWalletRegistration awr
                           on awr.PassTypeIdentifier = awp.PassTypeIdentifier
                          and awr.SerialNumber = awp.SerialNumber
                       where awp.CardID = cc.CardID
                       limit 1
                   ) as HasRegisteredAppleDevices
            from ClientCard cc
            inner join UserClient uc
                on uc.UserID = cc.UserID
            inner join Business b
                on b.BusinessID = cc.BusinessID
            where coalesce(cc.LastCheck, cc.CreationDate) >= @ChangedSince
              and (
                  nullif(cc.CardIDGoogle, '') is not null
                  or exists (
                      select 1
                      from AppleWalletPass awp
                      inner join AppleWalletRegistration awr
                          on awr.PassTypeIdentifier = awp.PassTypeIdentifier
                         and awr.SerialNumber = awp.SerialNumber
                      where awp.CardID = cc.CardID
                      limit 1
                  )
              )
            order by coalesce(cc.LastCheck, cc.CreationDate), cc.CardID
            limit @BatchSize;
            """;

        await using var connection = _connectionFactory.Create();
        var rows = await connection.QueryAsync<LegacyWalletSyncRow>(
            new CommandDefinition(sql, new
            {
                ChangedSince = changedSince.UtcDateTime,
                BatchSize = Math.Max(1, batchSize)
            }, cancellationToken: cancellationToken));

        return rows.Select(row => row.ToCandidate()).ToArray();
    }

    private static DateTimeOffset AsUtc(DateTime? value)
    {
        var dateTime = value ?? DateTime.UtcNow;
        return dateTime.Kind == DateTimeKind.Unspecified
            ? new DateTimeOffset(DateTime.SpecifyKind(dateTime, DateTimeKind.Utc))
            : new DateTimeOffset(dateTime.ToUniversalTime());
    }

    private sealed record LegacyWalletSyncRow(
        int CardID,
        string? CardIDGoogle,
        DateTime? CreationDate,
        int? CheckQTY,
        DateTime? LastCheck,
        int UserID,
        int BusinessID,
        int? HistoricCheckQTY,
        string UserName,
        string FirstName,
        string Lastname,
        string UserEmail,
        string BusinessName,
        string BusinessEmail,
        string BusinessPassword,
        string? BusinessLogo,
        long HasRegisteredAppleDevices)
    {
        public LegacyWalletSyncCandidate ToCandidate()
        {
            var cardGuid = LegacyIdMapper.ToGuid(CardID);
            var currentStamps = CheckQTY ?? 0;
            var lifetimeStamps = Math.Max(HistoricCheckQTY ?? currentStamps, currentStamps);
            var createdAt = AsUtc(CreationDate);
            var card = LoyaltyCard.Restore(
                cardGuid,
                LegacyIdMapper.ToGuid(UserID),
                LegacyIdMapper.ToGuid(BusinessID),
                cardGuid.ToString("N"),
                currentStamps,
                lifetimeStamps,
                createdAt,
                AsUtc(LastCheck ?? CreationDate),
                string.IsNullOrWhiteSpace(CardIDGoogle) ? null : CardIDGoogle,
                googleSaveUrl: null);

            var client = new Client(
                LegacyIdMapper.ToGuid(UserID),
                UserName,
                FirstName,
                Lastname,
                UserEmail);

            var business = new Business(
                LegacyIdMapper.ToGuid(BusinessID),
                BusinessName,
                BusinessEmail,
                BusinessPassword,
                string.IsNullOrWhiteSpace(BusinessLogo) ? "/img/demo-coffee.svg" : BusinessLogo);

            return new LegacyWalletSyncCandidate(card, client, business, HasRegisteredAppleDevices != 0);
        }
    }
}
