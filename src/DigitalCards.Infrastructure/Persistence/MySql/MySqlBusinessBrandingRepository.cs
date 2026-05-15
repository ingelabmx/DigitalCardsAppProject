using Dapper;
using DigitalCards.Application.Abstractions;
using DigitalCards.Domain;
using MySqlConnector;

namespace DigitalCards.Infrastructure.Persistence.MySql;

public sealed class MySqlBusinessBrandingRepository : IBusinessBrandingRepository
{
    private const int MissingTableErrorNumber = 1146;

    private readonly MySqlConnectionFactory _connectionFactory;

    public MySqlBusinessBrandingRepository(MySqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<BusinessBranding?> FindByBusinessIdAsync(
        Guid businessId,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            select BusinessID,
                   PublicName,
                   LogoPath,
                   PrimaryColor,
                   SecondaryColor,
                   CustomFieldColor,
                   ProgramName,
                   ProgramDescription,
                   UpdatedAt,
                   UpdatedByAdminUserID
            from ModernBusinessBranding
            where BusinessID = @BusinessId;
            """;

        try
        {
            await using var connection = _connectionFactory.Create();
            var row = await connection.QuerySingleOrDefaultAsync<BusinessBrandingRow>(
                new CommandDefinition(
                    sql,
                    new { BusinessId = LegacyIdMapper.ToInt32(businessId) },
                    cancellationToken: cancellationToken));

            return row?.ToDomain();
        }
        catch (MySqlException exception) when (exception.Number == MissingTableErrorNumber)
        {
            return null;
        }
    }

    public async Task UpsertAsync(BusinessBranding branding, CancellationToken cancellationToken = default)
    {
        const string sql = """
            insert into ModernBusinessBranding (
                BusinessID,
                PublicName,
                LogoPath,
                PrimaryColor,
                SecondaryColor,
                CustomFieldColor,
                ProgramName,
                ProgramDescription,
                UpdatedAt,
                UpdatedByAdminUserID)
            values (
                @BusinessId,
                @PublicName,
                @LogoPath,
                @PrimaryColor,
                @SecondaryColor,
                @CustomFieldColor,
                @ProgramName,
                @ProgramDescription,
                @UpdatedAt,
                @UpdatedByAdminUserId)
            on duplicate key update
                PublicName = values(PublicName),
                LogoPath = values(LogoPath),
                PrimaryColor = values(PrimaryColor),
                SecondaryColor = values(SecondaryColor),
                CustomFieldColor = values(CustomFieldColor),
                ProgramName = values(ProgramName),
                ProgramDescription = values(ProgramDescription),
                UpdatedAt = values(UpdatedAt),
                UpdatedByAdminUserID = values(UpdatedByAdminUserID);
            """;

        try
        {
            await using var connection = _connectionFactory.Create();
            await connection.ExecuteAsync(
                new CommandDefinition(
                    sql,
                    new
                    {
                        BusinessId = LegacyIdMapper.ToInt32(branding.BusinessId),
                        branding.PublicName,
                        branding.LogoPath,
                        branding.PrimaryColor,
                        branding.SecondaryColor,
                        branding.CustomFieldColor,
                        branding.ProgramName,
                        branding.ProgramDescription,
                        UpdatedAt = branding.UpdatedAt.UtcDateTime,
                        UpdatedByAdminUserId = branding.UpdatedByAdminUserId.HasValue
                            ? LegacyIdMapper.ToInt32(branding.UpdatedByAdminUserId.Value)
                            : (int?)null
                    },
                    cancellationToken: cancellationToken));
        }
        catch (MySqlException exception) when (exception.Number == MissingTableErrorNumber)
        {
            throw new InvalidOperationException(
                "ModernBusinessBranding table is missing. Apply docs/migration-context/31-business-branding-v1-hostgator.sql before editing branding.",
                exception);
        }
    }

    private sealed class BusinessBrandingRow
    {
        public int BusinessID { get; init; }

        public string PublicName { get; init; } = string.Empty;

        public string LogoPath { get; init; } = string.Empty;

        public string PrimaryColor { get; init; } = string.Empty;

        public string SecondaryColor { get; init; } = string.Empty;

        public string CustomFieldColor { get; init; } = "#FFFFFF";

        public string ProgramName { get; init; } = string.Empty;

        public string ProgramDescription { get; init; } = string.Empty;

        public DateTime UpdatedAt { get; init; }

        public int? UpdatedByAdminUserID { get; init; }

        public BusinessBranding ToDomain()
        {
            return new BusinessBranding(
                LegacyIdMapper.ToGuid(BusinessID),
                PublicName,
                LogoPath,
                PrimaryColor,
                SecondaryColor,
                CustomFieldColor,
                ProgramName,
                ProgramDescription,
                new DateTimeOffset(DateTime.SpecifyKind(UpdatedAt, DateTimeKind.Utc)),
                UpdatedByAdminUserID.HasValue
                    ? LegacyIdMapper.ToGuid(UpdatedByAdminUserID.Value)
                    : null);
        }
    }
}
