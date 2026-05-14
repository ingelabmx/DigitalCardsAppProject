using Dapper;
using DigitalCards.Application.Abstractions;
using DigitalCards.Application.Models;
using MySqlConnector;

namespace DigitalCards.Infrastructure.Persistence.MySql;

public sealed class MySqlAccountLifecycleRepository : IAccountLifecycleRepository
{
    private const int MissingTableErrorNumber = 1146;

    private readonly MySqlConnectionFactory _connectionFactory;

    public MySqlAccountLifecycleRepository(MySqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<ClientCardLifecycleRecord?> FindCardLifecycleAsync(
        Guid cardId,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            select CardID,
                   BusinessID,
                   IsActive,
                   UpdatedAt,
                   UpdatedByBusinessID
            from ModernClientCardStatus
            where CardID = @CardId
            limit 1;
            """;

        try
        {
            await using var connection = _connectionFactory.Create();
            var row = await connection.QuerySingleOrDefaultAsync<ClientCardStatusRow>(
                new CommandDefinition(
                    sql,
                    new { CardId = LegacyIdMapper.ToInt32(cardId) },
                    cancellationToken: cancellationToken));

            return row?.ToModel();
        }
        catch (MySqlException exception) when (exception.Number == MissingTableErrorNumber)
        {
            return null;
        }
    }

    public async Task SetCardActiveAsync(
        ClientCardLifecycleRecord status,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            insert into ModernClientCardStatus (
                CardID,
                BusinessID,
                IsActive,
                DisabledAt,
                UpdatedAt,
                UpdatedByBusinessID)
            values (
                @CardId,
                @BusinessId,
                @IsActive,
                @DisabledAt,
                @UpdatedAt,
                @UpdatedByBusinessId)
            on duplicate key update
                BusinessID = values(BusinessID),
                IsActive = values(IsActive),
                DisabledAt = values(DisabledAt),
                UpdatedAt = values(UpdatedAt),
                UpdatedByBusinessID = values(UpdatedByBusinessID);
            """;

        try
        {
            await using var connection = _connectionFactory.Create();
            await connection.ExecuteAsync(
                new CommandDefinition(
                    sql,
                    new
                    {
                        CardId = LegacyIdMapper.ToInt32(status.CardId),
                        BusinessId = LegacyIdMapper.ToInt32(status.BusinessId),
                        status.IsActive,
                        DisabledAt = status.IsActive ? (DateTime?)null : status.UpdatedAt.UtcDateTime,
                        UpdatedAt = status.UpdatedAt.UtcDateTime,
                        UpdatedByBusinessId = status.UpdatedByBusinessId.HasValue
                            ? LegacyIdMapper.ToInt32(status.UpdatedByBusinessId.Value)
                            : (int?)null
                    },
                    cancellationToken: cancellationToken));
        }
        catch (MySqlException exception) when (exception.Number == MissingTableErrorNumber)
        {
            throw new InvalidOperationException(
                "ModernClientCardStatus table is missing. Run docs/migration-context/94-account-lifecycle-and-delete-hostgator.sql before changing card lifecycle.",
                exception);
        }
    }

    public async Task<bool> DeleteBusinessCardAsync(
        Guid businessId,
        Guid cardId,
        CancellationToken cancellationToken = default)
    {
        await using var connection = _connectionFactory.Create();
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        var removed = await DeleteBusinessCardCoreAsync(
            connection,
            transaction,
            LegacyIdMapper.ToInt32(businessId),
            LegacyIdMapper.ToInt32(cardId),
            cancellationToken);

        await transaction.CommitAsync(cancellationToken);
        return removed;
    }

    public async Task<bool> DeleteBusinessAsync(
        Guid businessId,
        CancellationToken cancellationToken = default)
    {
        await using var connection = _connectionFactory.Create();
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        var legacyBusinessId = LegacyIdMapper.ToInt32(businessId);

        var cardIds = (await connection.QueryAsync<int>(
            new CommandDefinition(
                "select CardID from ClientCard where BusinessID = @BusinessId;",
                new { BusinessId = legacyBusinessId },
                transaction,
                cancellationToken: cancellationToken))).ToArray();

        foreach (var cardId in cardIds)
        {
            await DeleteBusinessCardCoreAsync(
                connection,
                transaction,
                legacyBusinessId,
                cardId,
                cancellationToken);
        }

        await ExecuteIgnoreMissingAsync(connection, transaction, "delete from ModernBusinessCredential where BusinessID = @BusinessId;", new { BusinessId = legacyBusinessId }, cancellationToken);
        await ExecuteIgnoreMissingAsync(connection, transaction, "delete from ModernPilotBusiness where BusinessID = @BusinessId;", new { BusinessId = legacyBusinessId }, cancellationToken);
        await ExecuteIgnoreMissingAsync(connection, transaction, "delete from ModernBusinessBranding where BusinessID = @BusinessId;", new { BusinessId = legacyBusinessId }, cancellationToken);
        await ExecuteIgnoreMissingAsync(connection, transaction, "delete from BusinessEnrollmentLinkToken where BusinessID = @BusinessId;", new { BusinessId = legacyBusinessId }, cancellationToken);
        await ExecuteIgnoreMissingAsync(connection, transaction, "delete from ModernPasswordResetToken where AccountType = 'Business' and AccountID = @BusinessId;", new { BusinessId = legacyBusinessId }, cancellationToken);
        await ExecuteIgnoreMissingAsync(connection, transaction, "delete from ModernClientConsent where BusinessID = @BusinessId;", new { BusinessId = legacyBusinessId }, cancellationToken);
        await ExecuteIgnoreMissingAsync(connection, transaction, "delete from ModernCutoverSmoke where BusinessID = @BusinessId;", new { BusinessId = legacyBusinessId }, cancellationToken);

        var removed = await connection.ExecuteAsync(
            new CommandDefinition(
                "delete from Business where BusinessID = @BusinessId;",
                new { BusinessId = legacyBusinessId },
                transaction,
                cancellationToken: cancellationToken)) > 0;

        await transaction.CommitAsync(cancellationToken);
        return removed;
    }

    private static async Task<bool> DeleteBusinessCardCoreAsync(
        MySqlConnection connection,
        MySqlTransaction transaction,
        int businessId,
        int cardId,
        CancellationToken cancellationToken)
    {
        var ownsCard = await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(
                "select count(*) from ClientCard where CardID = @CardId and BusinessID = @BusinessId;",
                new { CardId = cardId, BusinessId = businessId },
                transaction,
                cancellationToken: cancellationToken)) > 0;
        if (!ownsCard)
        {
            return false;
        }

        var passes = await ListApplePassesAsync(connection, transaction, cardId, cancellationToken);

        foreach (var pass in passes)
        {
            await ExecuteIgnoreMissingAsync(
                connection,
                transaction,
                "delete from AppleWalletRegistration where PassTypeIdentifier = @PassTypeIdentifier and SerialNumber = @SerialNumber;",
                pass,
                cancellationToken);
        }

        await ExecuteIgnoreMissingAsync(connection, transaction, "delete from AppleWalletPass where CardID = @CardId;", new { CardId = cardId }, cancellationToken);
        await ExecuteIgnoreMissingAsync(connection, transaction, "delete from WalletLinkToken where CardID = @CardId;", new { CardId = cardId }, cancellationToken);
        await ExecuteIgnoreMissingAsync(connection, transaction, "delete from StampLedger where CardID = @CardId;", new { CardId = cardId }, cancellationToken);
        await ExecuteIgnoreMissingAsync(connection, transaction, "delete from ModernClientCardStatus where CardID = @CardId;", new { CardId = cardId }, cancellationToken);
        await connection.ExecuteAsync(
            new CommandDefinition(
                "delete from ClientCard where CardID = @CardId and BusinessID = @BusinessId;",
                new { CardId = cardId, BusinessId = businessId },
                transaction,
                cancellationToken: cancellationToken));
        await ExecuteIgnoreMissingAsync(
            connection,
            transaction,
            """
            delete from AppleWalletDevice
            where not exists (
                select 1
                from AppleWalletRegistration
                where AppleWalletRegistration.DeviceLibraryIdentifier = AppleWalletDevice.DeviceLibraryIdentifier
            );
            """,
            null,
            cancellationToken);

        return true;
    }

    private static async Task<IReadOnlyList<ApplePassKey>> ListApplePassesAsync(
        MySqlConnection connection,
        MySqlTransaction transaction,
        int cardId,
        CancellationToken cancellationToken)
    {
        try
        {
            return (await connection.QueryAsync<ApplePassKey>(
                new CommandDefinition(
                    "select PassTypeIdentifier, SerialNumber from AppleWalletPass where CardID = @CardId;",
                    new { CardId = cardId },
                    transaction,
                    cancellationToken: cancellationToken))).ToArray();
        }
        catch (MySqlException exception) when (exception.Number == MissingTableErrorNumber)
        {
            return [];
        }
    }

    private static async Task ExecuteIgnoreMissingAsync(
        MySqlConnection connection,
        MySqlTransaction transaction,
        string sql,
        object? parameters,
        CancellationToken cancellationToken)
    {
        try
        {
            await connection.ExecuteAsync(
                new CommandDefinition(sql, parameters, transaction, cancellationToken: cancellationToken));
        }
        catch (MySqlException exception) when (exception.Number == MissingTableErrorNumber)
        {
        }
    }

    private sealed record ApplePassKey(string PassTypeIdentifier, string SerialNumber);

    private sealed record ClientCardStatusRow(
        int CardID,
        int BusinessID,
        bool IsActive,
        DateTime UpdatedAt,
        int? UpdatedByBusinessID)
    {
        public ClientCardLifecycleRecord ToModel()
        {
            return new ClientCardLifecycleRecord(
                LegacyIdMapper.ToGuid(CardID),
                LegacyIdMapper.ToGuid(BusinessID),
                IsActive,
                new DateTimeOffset(DateTime.SpecifyKind(UpdatedAt, DateTimeKind.Utc)),
                UpdatedByBusinessID.HasValue ? LegacyIdMapper.ToGuid(UpdatedByBusinessID.Value) : null);
        }
    }
}
