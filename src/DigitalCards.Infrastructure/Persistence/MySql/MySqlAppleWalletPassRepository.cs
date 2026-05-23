using Dapper;
using DigitalCards.Application.Abstractions;
using DigitalCards.Application.Models;

namespace DigitalCards.Infrastructure.Persistence.MySql;

public sealed class MySqlAppleWalletPassRepository : IAppleWalletPassRepository
{
    private readonly MySqlConnectionFactory _connectionFactory;

    public MySqlAppleWalletPassRepository(MySqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task UpsertPassAsync(AppleWalletPassRecord pass, CancellationToken cancellationToken = default)
    {
        const string sql = """
            insert into AppleWalletPass (PassTypeIdentifier, SerialNumber, CardID, AuthTokenHash, UpdateTag, CreatedAt, UpdatedAt)
            values (@PassTypeIdentifier, @SerialNumber, @CardID, @AuthenticationTokenHash, @UpdateTag, @CreatedAt, @UpdatedAt)
            on duplicate key update
                CardID = values(CardID),
                AuthTokenHash = values(AuthTokenHash),
                UpdateTag = values(UpdateTag),
                UpdatedAt = values(UpdatedAt);
            """;

        await using var connection = _connectionFactory.Create();
        await connection.ExecuteAsync(new CommandDefinition(sql, ToParameters(pass), cancellationToken: cancellationToken));
    }

    public async Task<AppleWalletPassRecord?> FindPassAsync(
        string passTypeIdentifier,
        string serialNumber,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            select PassTypeIdentifier,
                   SerialNumber,
                   CardID,
                   AuthTokenHash,
                   UpdateTag,
                   CreatedAt,
                   UpdatedAt
            from AppleWalletPass
            where PassTypeIdentifier = @PassTypeIdentifier
              and SerialNumber = @SerialNumber
            limit 1;
            """;

        await using var connection = _connectionFactory.Create();
        var row = await connection.QuerySingleOrDefaultAsync<AppleWalletPassRow>(
            new CommandDefinition(sql, new { PassTypeIdentifier = passTypeIdentifier, SerialNumber = serialNumber }, cancellationToken: cancellationToken));

        return row?.ToModel();
    }

    public async Task<AppleWalletPassRecord?> FindPassByCardIdAsync(Guid cardId, CancellationToken cancellationToken = default)
    {
        const string sql = """
            select PassTypeIdentifier,
                   SerialNumber,
                   CardID,
                   AuthTokenHash,
                   UpdateTag,
                   CreatedAt,
                   UpdatedAt
            from AppleWalletPass
            where CardID = @CardID
            limit 1;
            """;

        await using var connection = _connectionFactory.Create();
        var row = await connection.QuerySingleOrDefaultAsync<AppleWalletPassRow>(
            new CommandDefinition(sql, new { CardID = LegacyIdMapper.ToInt32(cardId) }, cancellationToken: cancellationToken));

        return row?.ToModel();
    }

    public async Task UpdatePassTagAsync(
        string passTypeIdentifier,
        string serialNumber,
        string updateTag,
        DateTimeOffset updatedAt,
        CancellationToken cancellationToken = default)
    {
        // GREATEST ensures the tag is strictly increasing even when two operations
        // happen within the same millisecond (redeem + immediate stamp race condition).
        const string sql = """
            update AppleWalletPass
            set UpdateTag = cast(GREATEST(cast(@UpdateTag as unsigned), cast(UpdateTag as unsigned) + 1) as char),
                UpdatedAt = @UpdatedAt
            where PassTypeIdentifier = @PassTypeIdentifier
              and SerialNumber = @SerialNumber;
            """;

        await using var connection = _connectionFactory.Create();
        await connection.ExecuteAsync(new CommandDefinition(sql, new
        {
            PassTypeIdentifier = passTypeIdentifier,
            SerialNumber = serialNumber,
            UpdateTag = updateTag,
            UpdatedAt = updatedAt.UtcDateTime
        }, cancellationToken: cancellationToken));
    }

    public async Task UpsertDeviceAsync(AppleWalletDeviceRecord device, CancellationToken cancellationToken = default)
    {
        const string sql = """
            insert into AppleWalletDevice (DeviceLibraryIdentifier, PushToken, CreatedAt, UpdatedAt)
            values (@DeviceLibraryIdentifier, @PushToken, @CreatedAt, @UpdatedAt)
            on duplicate key update
                PushToken = values(PushToken),
                UpdatedAt = values(UpdatedAt);
            """;

        await using var connection = _connectionFactory.Create();
        await connection.ExecuteAsync(new CommandDefinition(sql, new
        {
            device.DeviceLibraryIdentifier,
            device.PushToken,
            CreatedAt = device.CreatedAt.UtcDateTime,
            UpdatedAt = device.UpdatedAt.UtcDateTime
        }, cancellationToken: cancellationToken));
    }

    public async Task<bool> AddRegistrationAsync(
        string deviceLibraryIdentifier,
        string passTypeIdentifier,
        string serialNumber,
        DateTimeOffset createdAt,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            insert ignore into AppleWalletRegistration (DeviceLibraryIdentifier, PassTypeIdentifier, SerialNumber, CreatedAt)
            values (@DeviceLibraryIdentifier, @PassTypeIdentifier, @SerialNumber, @CreatedAt);
            """;

        await using var connection = _connectionFactory.Create();
        var affected = await connection.ExecuteAsync(new CommandDefinition(sql, new
        {
            DeviceLibraryIdentifier = deviceLibraryIdentifier,
            PassTypeIdentifier = passTypeIdentifier,
            SerialNumber = serialNumber,
            CreatedAt = createdAt.UtcDateTime
        }, cancellationToken: cancellationToken));

        return affected > 0;
    }

    public async Task<bool> RemoveRegistrationAsync(
        string deviceLibraryIdentifier,
        string passTypeIdentifier,
        string serialNumber,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            delete from AppleWalletRegistration
            where DeviceLibraryIdentifier = @DeviceLibraryIdentifier
              and PassTypeIdentifier = @PassTypeIdentifier
              and SerialNumber = @SerialNumber;
            """;

        await using var connection = _connectionFactory.Create();
        var affected = await connection.ExecuteAsync(new CommandDefinition(sql, new
        {
            DeviceLibraryIdentifier = deviceLibraryIdentifier,
            PassTypeIdentifier = passTypeIdentifier,
            SerialNumber = serialNumber
        }, cancellationToken: cancellationToken));

        return affected > 0;
    }

    public async Task DeleteDeviceIfOrphanedAsync(string deviceLibraryIdentifier, CancellationToken cancellationToken = default)
    {
        const string sql = """
            delete from AppleWalletDevice
            where DeviceLibraryIdentifier = @DeviceLibraryIdentifier
              and not exists (
                  select 1
                  from AppleWalletRegistration
                  where AppleWalletRegistration.DeviceLibraryIdentifier = AppleWalletDevice.DeviceLibraryIdentifier
              );
            """;

        await using var connection = _connectionFactory.Create();
        await connection.ExecuteAsync(new CommandDefinition(sql, new
        {
            DeviceLibraryIdentifier = deviceLibraryIdentifier
        }, cancellationToken: cancellationToken));
    }

    public async Task<IReadOnlyList<AppleWalletPassRecord>> ListUpdatedPassesForDeviceAsync(
        string deviceLibraryIdentifier,
        string passTypeIdentifier,
        string? previousLastUpdated,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            select p.PassTypeIdentifier,
                   p.SerialNumber,
                   p.CardID,
                   p.AuthTokenHash,
                   p.UpdateTag,
                   p.CreatedAt,
                   p.UpdatedAt
            from AppleWalletPass p
            inner join AppleWalletRegistration r
                on r.PassTypeIdentifier = p.PassTypeIdentifier
               and r.SerialNumber = p.SerialNumber
            where r.DeviceLibraryIdentifier = @DeviceLibraryIdentifier
              and p.PassTypeIdentifier = @PassTypeIdentifier
              and (@PreviousLastUpdated is null or cast(p.UpdateTag as unsigned) > cast(@PreviousLastUpdated as unsigned))
            order by cast(p.UpdateTag as unsigned), p.SerialNumber;
            """;

        await using var connection = _connectionFactory.Create();
        var rows = await connection.QueryAsync<AppleWalletPassRow>(
            new CommandDefinition(sql, new
            {
                DeviceLibraryIdentifier = deviceLibraryIdentifier,
                PassTypeIdentifier = passTypeIdentifier,
                PreviousLastUpdated = string.IsNullOrWhiteSpace(previousLastUpdated) ? null : previousLastUpdated
            }, cancellationToken: cancellationToken));

        return rows.Select(row => row.ToModel()).ToArray();
    }

    public async Task<IReadOnlyList<AppleWalletDeviceRecord>> ListDevicesForPassAsync(
        string passTypeIdentifier,
        string serialNumber,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            select d.DeviceLibraryIdentifier,
                   d.PushToken,
                   d.CreatedAt,
                   d.UpdatedAt
            from AppleWalletDevice d
            inner join AppleWalletRegistration r
                on r.DeviceLibraryIdentifier = d.DeviceLibraryIdentifier
            where r.PassTypeIdentifier = @PassTypeIdentifier
              and r.SerialNumber = @SerialNumber;
            """;

        await using var connection = _connectionFactory.Create();
        var rows = await connection.QueryAsync<AppleWalletDeviceRow>(
            new CommandDefinition(sql, new
            {
                PassTypeIdentifier = passTypeIdentifier,
                SerialNumber = serialNumber
            }, cancellationToken: cancellationToken));

        return rows.Select(row => row.ToModel()).ToArray();
    }

    private static object ToParameters(AppleWalletPassRecord pass)
    {
        return new
        {
            pass.PassTypeIdentifier,
            pass.SerialNumber,
            CardID = LegacyIdMapper.ToInt32(pass.CardId),
            pass.AuthenticationTokenHash,
            pass.UpdateTag,
            CreatedAt = pass.CreatedAt.UtcDateTime,
            UpdatedAt = pass.UpdatedAt.UtcDateTime
        };
    }

    private static DateTimeOffset AsUtc(DateTime value)
    {
        return value.Kind == DateTimeKind.Unspecified
            ? new DateTimeOffset(DateTime.SpecifyKind(value, DateTimeKind.Utc))
            : new DateTimeOffset(value.ToUniversalTime());
    }

    private sealed record AppleWalletPassRow(
        string PassTypeIdentifier,
        string SerialNumber,
        int CardID,
        string AuthTokenHash,
        string UpdateTag,
        DateTime CreatedAt,
        DateTime UpdatedAt)
    {
        public AppleWalletPassRecord ToModel()
        {
            return new AppleWalletPassRecord(
                PassTypeIdentifier,
                SerialNumber,
                LegacyIdMapper.ToGuid(CardID),
                AuthTokenHash,
                UpdateTag,
                AsUtc(CreatedAt),
                AsUtc(UpdatedAt));
        }
    }

    private sealed record AppleWalletDeviceRow(
        string DeviceLibraryIdentifier,
        string PushToken,
        DateTime CreatedAt,
        DateTime UpdatedAt)
    {
        public AppleWalletDeviceRecord ToModel()
        {
            return new AppleWalletDeviceRecord(
                DeviceLibraryIdentifier,
                PushToken,
                AsUtc(CreatedAt),
                AsUtc(UpdatedAt));
        }
    }
}
