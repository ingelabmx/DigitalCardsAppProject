using Dapper;
using DigitalCards.Application.Abstractions;
using DigitalCards.Application.Models;
using DigitalCards.Domain;
using MySqlConnector;

namespace DigitalCards.Infrastructure.Persistence.MySql;

public sealed class MySqlLegacyWalletSyncRepository : ILegacyWalletSyncRepository
{
    private const int MissingTableErrorNumber = 1146;

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
        const string brandedSql = """
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
                   mb.PublicName as BrandingPublicName,
                   mb.LogoPath as BrandingLogoPath,
                   mb.PrimaryColor as BrandingPrimaryColor,
                   mb.SecondaryColor as BrandingSecondaryColor,
                   mb.CustomFieldColor as BrandingCustomFieldColor,
                   mb.StampGoal as BrandingStampGoal,
                   mb.ProgramName as BrandingProgramName,
                   mb.ProgramDescription as BrandingProgramDescription,
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
            left join ModernBusinessBranding mb
                on mb.BusinessID = b.BusinessID
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

        const string legacySql = """
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
        var parameters = new
        {
            ChangedSince = changedSince.UtcDateTime,
            BatchSize = Math.Max(1, batchSize)
        };

        IEnumerable<LegacyWalletSyncRow> rows;
        try
        {
            rows = await connection.QueryAsync<LegacyWalletSyncRow>(
                new CommandDefinition(brandedSql, parameters, cancellationToken: cancellationToken));
        }
        catch (MySqlException exception) when (exception.Number == MissingTableErrorNumber)
        {
            rows = await connection.QueryAsync<LegacyWalletSyncRow>(
                new CommandDefinition(legacySql, parameters, cancellationToken: cancellationToken));
        }

        return rows.Select(row => row.ToCandidate()).ToArray();
    }

    private static DateTimeOffset AsUtc(DateTime? value)
    {
        var dateTime = value ?? DateTime.UtcNow;
        return dateTime.Kind == DateTimeKind.Unspecified
            ? new DateTimeOffset(DateTime.SpecifyKind(dateTime, DateTimeKind.Utc))
            : new DateTimeOffset(dateTime.ToUniversalTime());
    }

    private sealed class LegacyWalletSyncRow
    {
        public LegacyWalletSyncRow()
        {
        }

        public int CardID { get; set; }

        public string? CardIDGoogle { get; set; }

        public DateTime? CreationDate { get; set; }

        public int? CheckQTY { get; set; }

        public DateTime? LastCheck { get; set; }

        public int UserID { get; set; }

        public int BusinessID { get; set; }

        public int? HistoricCheckQTY { get; set; }

        public string UserName { get; set; } = string.Empty;

        public string FirstName { get; set; } = string.Empty;

        public string Lastname { get; set; } = string.Empty;

        public string UserEmail { get; set; } = string.Empty;

        public string BusinessName { get; set; } = string.Empty;

        public string BusinessEmail { get; set; } = string.Empty;

        public string BusinessPassword { get; set; } = string.Empty;

        public string? BusinessLogo { get; set; }

        public string? BrandingPublicName { get; set; }

        public string? BrandingLogoPath { get; set; }

        public string? BrandingPrimaryColor { get; set; }

        public string? BrandingSecondaryColor { get; set; }

        public string? BrandingCustomFieldColor { get; set; }

        public int? BrandingStampGoal { get; set; }

        public string? BrandingProgramName { get; set; }

        public string? BrandingProgramDescription { get; set; }

        public int HasRegisteredAppleDevices { get; set; }

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
                FirstNonEmpty(BrandingLogoPath, BusinessLogo, "/img/demo-coffee.svg"),
                publicName: BrandingPublicName,
                primaryColor: BrandingPrimaryColor,
                secondaryColor: BrandingSecondaryColor,
                programName: BrandingProgramName,
                programDescription: BrandingProgramDescription,
                customFieldColor: BrandingCustomFieldColor,
                stampGoal: BrandingStampGoal ?? Business.DefaultStampGoal);

            return new LegacyWalletSyncCandidate(card, client, business, HasRegisteredAppleDevices != 0);
        }

        private static string FirstNonEmpty(params string?[] values)
        {
            return values.First(value => !string.IsNullOrWhiteSpace(value))!;
        }
    }
}
