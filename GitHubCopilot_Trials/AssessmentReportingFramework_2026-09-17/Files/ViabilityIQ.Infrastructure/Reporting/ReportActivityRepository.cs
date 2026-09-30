using Dapper;
using Microsoft.Extensions.Logging;
using ViabilityIQ.Application.Interfaces;
using ViabilityIQ.Infrastructure.DbFactory;
using ViabilityIQ.Shared.Reporting;

namespace ViabilityIQ.Infrastructure.Reporting;

public sealed class ReportActivityRepository : IReportActivityRepository
{
    private readonly IDbConnectionFactory _connections;
    private readonly ILogger<ReportActivityRepository> _logger;

    public ReportActivityRepository(
        IDbConnectionFactory connections,
        ILogger<ReportActivityRepository> logger)
    {
        _connections = connections;
        _logger = logger;
    }

    public async Task<ReportActivityWriteResult> RecordAsync(
        ReportActivity activity, CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO dbo.tblReportActivity
            (
                ReportScope, ReportCode, ReportName, ReportVersion, AssessmentId,
                UserId, ActivityType, OutputFormat, RecipientMasked, Succeeded,
                ErrorCategory, CorrelationId, CreatedAtUtc
            )
            VALUES
            (
                @ReportScope, @ReportCode, @ReportName, @ReportVersion, @AssessmentId,
                @UserId, @ActivityType, @OutputFormat, @RecipientMasked, @Succeeded,
                @ErrorCategory, @CorrelationId, @CreatedAtUtc
            );
            """;
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            using var connection = _connections.CreateConnection();
            await connection.ExecuteAsync(sql, new
            {
                ReportScope = activity.Scope.ToString(),
                activity.ReportCode,
                activity.ReportName,
                activity.ReportVersion,
                activity.AssessmentId,
                activity.UserId,
                ActivityType = activity.ActivityType.ToString(),
                OutputFormat = activity.OutputFormat?.ToString(),
                activity.RecipientMasked,
                activity.Succeeded,
                activity.ErrorCategory,
                activity.CorrelationId,
                activity.CreatedAtUtc
            });
            return new(true, null);
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception,
                "Report activity audit unavailable for {ReportCode} correlation {CorrelationId}",
                activity.ReportCode, activity.CorrelationId);
            return new(false, "The report completed, but its activity could not be written to the audit table.");
        }
    }
}

