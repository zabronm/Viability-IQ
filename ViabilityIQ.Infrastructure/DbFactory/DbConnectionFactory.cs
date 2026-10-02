using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.Data;

namespace ViabilityIQ.Infrastructure.DbFactory;

public class DbConnectionFactory : IDbConnectionFactory
{
    private readonly string _connectionString;

    public DbConnectionFactory(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var configuredConnectionString = configuration.GetConnectionString("ViabilityIQ_Connection");
        if (string.IsNullOrWhiteSpace(configuredConnectionString))
        {
            throw new InvalidOperationException(
                "Connection string 'ViabilityIQ_Connection' is not configured.");
        }

        var connectionStringBuilder = new SqlConnectionStringBuilder(configuredConnectionString)
        {
            ConnectTimeout = ReadPositiveInt(
                configuration,
                "DatabaseResilience:ConnectionTimeoutSeconds",
                15),
            ConnectRetryCount = ReadNonNegativeInt(
                configuration,
                "DatabaseResilience:ConnectionRetryCount",
                3),
            ConnectRetryInterval = ReadPositiveInt(
                configuration,
                "DatabaseResilience:ConnectionRetryIntervalSeconds",
                2)
        };

        _connectionString = connectionStringBuilder.ConnectionString;
        SqlMapper.Settings.CommandTimeout = ReadPositiveInt(
            configuration,
            "DatabaseResilience:CommandTimeoutSeconds",
            60);
    }

    public IDbConnection CreateConnection() => new SqlConnection(_connectionString);

    private static int ReadPositiveInt(
        IConfiguration configuration,
        string key,
        int defaultValue) =>
        int.TryParse(configuration[key], out var value) && value > 0
            ? value
            : defaultValue;

    private static int ReadNonNegativeInt(
        IConfiguration configuration,
        string key,
        int defaultValue) =>
        int.TryParse(configuration[key], out var value) && value >= 0
            ? value
            : defaultValue;
}
