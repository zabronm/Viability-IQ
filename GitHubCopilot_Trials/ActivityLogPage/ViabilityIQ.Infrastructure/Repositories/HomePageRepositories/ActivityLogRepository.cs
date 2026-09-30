using Dapper;
using Microsoft.Extensions.Logging;
using ViabilityIQ.Application.Interfaces.HomePageInterfaces;
using ViabilityIQ.Infrastructure.DbFactory;
using ViabilityIQ.Shared.DataModels.HomePageModels;

namespace ViabilityIQ.Infrastructure.Repositories.HomePageRepositories;

public sealed class ActivityLogRepository : IActivityLogRepository
{
    private const int MaximumPageSize = 100;
    private readonly IDbConnectionFactory _dbConnectionFactory;
    private readonly ILogger<ActivityLogRepository> _logger;

    public ActivityLogRepository(
        IDbConnectionFactory dbConnectionFactory,
        ILogger<ActivityLogRepository> logger)
    {
        _dbConnectionFactory = dbConnectionFactory;
        _logger = logger;
    }

    public async Task<List<ActivityLogModel>> GetRecentActivitiesAsync(
        long userId,
        int count = 3,
        string filterType = "all")
    {
        var safeCount = Math.Clamp(count, 1, 20);
        var fromUtc = filterType switch
        {
            "24h" => DateTime.UtcNow.AddHours(-24),
            "7d" => DateTime.UtcNow.AddDays(-7),
            "30d" => DateTime.UtcNow.AddDays(-30),
            _ => (DateTime?)null
        };

        const string sql = """
            SELECT TOP (@Count)
                al.ActivityLogId AS Id,
                al.UserId,
                COALESCE(NULLIF(al.ActorName, ''), 'System') AS ActorName,
                al.ActivityAction,
                al.EntityType,
                al.EntityId,
                COALESCE(al.EntityName, '') AS EntityName,
                al.AssessmentId,
                COALESCE(al.AssessmentName, '') AS AssessmentName,
                COALESCE(al.Module, '') AS Module,
                COALESCE(al.Page, '') AS Page,
                COALESCE(al.Remarks, '') AS Remarks,
                al.CreatedDate
            FROM dbo.tblActivityLog al
            WHERE ISNULL(al.Active, 1) = 1
              AND al.UserId = @UserId
              AND (@FromUtc IS NULL OR al.CreatedDate >= @FromUtc)
            ORDER BY al.CreatedDate DESC, al.ActivityLogId DESC;
            """;

        try
        {
            using var connection = _dbConnectionFactory.CreateConnection();
            var activities = (await connection.QueryAsync<ActivityLogModel>(
                sql,
                new { UserId = userId, Count = safeCount, FromUtc = fromUtc })).ToList();

            AddNavigationUrls(activities);
            return activities;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve recent activities for user {UserId}", userId);
            throw;
        }
    }

    public async Task<List<ActivityLogModel>> GetActivitiesByDateRangeAsync(
        long userId,
        DateTime startDate,
        DateTime endDate)
    {
        if (endDate < startDate)
        {
            throw new ArgumentException("The activity end date cannot precede its start date.");
        }

        const string sql = """
            SELECT
                al.ActivityLogId AS Id,
                al.UserId,
                COALESCE(NULLIF(al.ActorName, ''), 'System') AS ActorName,
                al.ActivityAction,
                al.EntityType,
                al.EntityId,
                COALESCE(al.EntityName, '') AS EntityName,
                al.AssessmentId,
                COALESCE(al.AssessmentName, '') AS AssessmentName,
                COALESCE(al.Module, '') AS Module,
                COALESCE(al.Page, '') AS Page,
                COALESCE(al.Remarks, '') AS Remarks,
                al.CreatedDate
            FROM dbo.tblActivityLog al
            WHERE ISNULL(al.Active, 1) = 1
              AND al.UserId = @UserId
              AND al.CreatedDate >= @StartDate
              AND al.CreatedDate <= @EndDate
            ORDER BY al.CreatedDate DESC, al.ActivityLogId DESC;
            """;

        try
        {
            using var connection = _dbConnectionFactory.CreateConnection();
            var activities = (await connection.QueryAsync<ActivityLogModel>(
                sql,
                new { UserId = userId, StartDate = startDate, EndDate = endDate })).ToList();

            AddNavigationUrls(activities);
            return activities;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to retrieve activities for user {UserId} between {StartDate} and {EndDate}",
                userId,
                startDate,
                endDate);
            throw;
        }
    }

    public async Task<ActivityLogPageResult> GetActivityPageAsync(ActivityLogQueryModel query)
    {
        ArgumentNullException.ThrowIfNull(query);

        var pageNumber = Math.Max(query.PageNumber, 1);
        var pageSize = Math.Clamp(query.PageSize, 1, MaximumPageSize);
        var offset = (pageNumber - 1) * pageSize;
        var searchPattern = string.IsNullOrWhiteSpace(query.SearchText)
            ? null
            : $"%{query.SearchText.Trim()}%";
        var todayUtc = DateTime.UtcNow.Date;
        var sevenDaysUtc = todayUtc.AddDays(-6);

        const string sql = """
            SELECT
                al.ActivityLogId AS Id,
                al.UserId,
                COALESCE(NULLIF(al.ActorName, ''), 'System') AS ActorName,
                al.ActivityAction,
                al.EntityType,
                al.EntityId,
                COALESCE(al.EntityName, '') AS EntityName,
                al.AssessmentId,
                COALESCE(al.AssessmentName, '') AS AssessmentName,
                COALESCE(al.Module, '') AS Module,
                COALESCE(al.Page, '') AS Page,
                COALESCE(al.IpAddress, '') AS IpAddress,
                COALESCE(al.UserAgent, '') AS UserAgent,
                COALESCE(al.CorrelationId, '') AS CorrelationId,
                COALESCE(al.MetadataJson, '') AS MetadataJson,
                COALESCE(al.Remarks, '') AS Remarks,
                al.CreatedDate
            FROM dbo.tblActivityLog al
            WHERE ISNULL(al.Active, 1) = 1
              AND al.UserId = @UserId
              AND (@ActivityAction IS NULL OR al.ActivityAction = @ActivityAction)
              AND (@EntityType IS NULL OR al.EntityType = @EntityType)
              AND (@FromUtc IS NULL OR al.CreatedDate >= @FromUtc)
              AND (@ToUtcExclusive IS NULL OR al.CreatedDate < @ToUtcExclusive)
              AND (
                    @SearchPattern IS NULL
                    OR al.ActorName LIKE @SearchPattern
                    OR al.EntityName LIKE @SearchPattern
                    OR al.AssessmentName LIKE @SearchPattern
                    OR al.Remarks LIKE @SearchPattern
                    OR al.Module LIKE @SearchPattern
                  )
            ORDER BY al.CreatedDate DESC, al.ActivityLogId DESC
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;

            SELECT COUNT_BIG(1)
            FROM dbo.tblActivityLog al
            WHERE ISNULL(al.Active, 1) = 1
              AND al.UserId = @UserId
              AND (@ActivityAction IS NULL OR al.ActivityAction = @ActivityAction)
              AND (@EntityType IS NULL OR al.EntityType = @EntityType)
              AND (@FromUtc IS NULL OR al.CreatedDate >= @FromUtc)
              AND (@ToUtcExclusive IS NULL OR al.CreatedDate < @ToUtcExclusive)
              AND (
                    @SearchPattern IS NULL
                    OR al.ActorName LIKE @SearchPattern
                    OR al.EntityName LIKE @SearchPattern
                    OR al.AssessmentName LIKE @SearchPattern
                    OR al.Remarks LIKE @SearchPattern
                    OR al.Module LIKE @SearchPattern
                  );

            SELECT
                COUNT_BIG(1) AS TotalCount,
                COALESCE(SUM(CASE WHEN al.CreatedDate >= @TodayUtc THEN 1 ELSE 0 END), 0) AS TodayCount,
                COALESCE(SUM(CASE WHEN al.CreatedDate >= @SevenDaysUtc THEN 1 ELSE 0 END), 0) AS LastSevenDaysCount,
                COUNT(DISTINCT COALESCE(CONVERT(nvarchar(30), al.UserId), NULLIF(al.ActorName, ''))) AS ActorCount
            FROM dbo.tblActivityLog al
            WHERE ISNULL(al.Active, 1) = 1
              AND al.UserId = @UserId;
            """;

        var parameters = new
        {
            query.UserId,
            ActivityAction = NullIfWhiteSpace(query.ActivityAction),
            EntityType = NullIfWhiteSpace(query.EntityType),
            query.FromUtc,
            query.ToUtcExclusive,
            SearchPattern = searchPattern,
            Offset = offset,
            PageSize = pageSize,
            TodayUtc = todayUtc,
            SevenDaysUtc = sevenDaysUtc
        };

        try
        {
            using var connection = _dbConnectionFactory.CreateConnection();
            using var resultSets = await connection.QueryMultipleAsync(sql, parameters);

            var items = (await resultSets.ReadAsync<ActivityLogModel>()).ToList();
            var filteredCount = checked((int)await resultSets.ReadSingleAsync<long>());
            var summary = await resultSets.ReadSingleAsync<ActivityLogSummaryRow>();
            AddNavigationUrls(items);

            return new ActivityLogPageResult
            {
                Items = items,
                FilteredCount = filteredCount,
                TotalCount = checked((int)summary.TotalCount),
                TodayCount = checked((int)summary.TodayCount),
                LastSevenDaysCount = checked((int)summary.LastSevenDaysCount),
                ActorCount = summary.ActorCount
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve activity page for user {UserId}", query.UserId);
            throw;
        }
    }

    private static string? NullIfWhiteSpace(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static void AddNavigationUrls(IEnumerable<ActivityLogModel> activities)
    {
        foreach (var activity in activities)
        {
            activity.NavigationUrl = activity.AssessmentId is > 0
                ? $"/assessment/dashboards/{activity.AssessmentId}"
                : null;
        }
    }

    private sealed class ActivityLogSummaryRow
    {
        public long TotalCount { get; set; }
        public long TodayCount { get; set; }
        public long LastSevenDaysCount { get; set; }
        public int ActorCount { get; set; }
    }
}
