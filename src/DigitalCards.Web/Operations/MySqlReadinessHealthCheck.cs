using Microsoft.Extensions.Diagnostics.HealthChecks;
using MySqlConnector;

namespace DigitalCards.Web.Operations;

public sealed class MySqlReadinessHealthCheck : IHealthCheck
{
    private readonly IConfiguration _configuration;

    public MySqlReadinessHealthCheck(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var provider = _configuration["DigitalCards:PersistenceProvider"];
        if (!string.Equals(provider, "MySql", StringComparison.OrdinalIgnoreCase))
        {
            return HealthCheckResult.Healthy("MySQL provider is not active.");
        }

        var connectionString = _configuration.GetConnectionString("DigitalCards");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return HealthCheckResult.Unhealthy("MySQL configuration is incomplete.");
        }

        try
        {
            await using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken);

            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT 1";
            command.CommandTimeout = 5;
            await command.ExecuteScalarAsync(cancellationToken);

            return HealthCheckResult.Healthy("MySQL connection succeeded.");
        }
        catch (Exception)
        {
            return HealthCheckResult.Unhealthy("MySQL readiness query failed.");
        }
    }
}
