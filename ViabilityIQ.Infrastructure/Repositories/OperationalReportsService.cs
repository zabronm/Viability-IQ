using System.Text;
using Dapper;
using Microsoft.Extensions.Logging;
using ViabilityIQ.Application.Interfaces;
using ViabilityIQ.Infrastructure.DbFactory;
using ViabilityIQ.Shared.Reporting;

namespace ViabilityIQ.Infrastructure.Repositories;

public sealed class OperationalReportsService : IOperationalReportsService
{
    private readonly IDbConnectionFactory _dbConnectionFactory;
    private readonly ITenantAuthorizationService _tenantAuthorizationService;
    private readonly ILogger<OperationalReportsService> _logger;

    public OperationalReportsService(
        IDbConnectionFactory dbConnectionFactory,
        ITenantAuthorizationService tenantAuthorizationService,
        ILogger<OperationalReportsService> logger)
    {
        _dbConnectionFactory = dbConnectionFactory;
        _tenantAuthorizationService = tenantAuthorizationService;
        _logger = logger;
    }

    public async Task<OperationalReportFilterOptions> GetAssessmentFilterOptionsAsync(
        CancellationToken cancellationToken = default)
    {
        await _tenantAuthorizationService.EnsureCanReadOperationalDataAsync(cancellationToken);
        var context = await _tenantAuthorizationService.GetAccessContextAsync(cancellationToken);

        const string sql = """
            WITH AuthorisedAssessments AS
            (
                SELECT assessment.AssessmentId, assessment.CreatedBy
                FROM tblAssessments assessment
                WHERE assessment.TenantId = @TenantId
                  AND
                  (
                      @CanReadAll = 1
                      OR
                      (
                          @CanReadScoped = 1
                          AND
                          (
                              assessment.CreatedBy = @UserId
                              OR EXISTS
                              (
                                  SELECT 1
                                  FROM tblTenantRecordGrant grantRecord
                                  WHERE grantRecord.TenantId = @TenantId
                                    AND grantRecord.TenantMembershipId = @MembershipId
                                    AND grantRecord.EntityType = 'Assessment'
                                    AND grantRecord.EntityId = assessment.AssessmentId
                                    AND grantRecord.Active = 1
                                    AND grantRecord.CanView = 1
                              )
                          )
                      )
                  )
            )
            SELECT DISTINCT
                branch.BranchId AS Id,
                branch.BranchName AS Name
            FROM AuthorisedAssessments authorised
            INNER JOIN tblApplicationUsers advisor ON advisor.UserId = authorised.CreatedBy
            INNER JOIN tblBranch branch ON branch.BranchId = advisor.BranchId
            WHERE branch.Active = 1
            ORDER BY branch.BranchName;

            WITH AuthorisedAssessments AS
            (
                SELECT assessment.AssessmentId, assessment.CreatedBy
                FROM tblAssessments assessment
                WHERE assessment.TenantId = @TenantId
                  AND
                  (
                      @CanReadAll = 1
                      OR
                      (
                          @CanReadScoped = 1
                          AND
                          (
                              assessment.CreatedBy = @UserId
                              OR EXISTS
                              (
                                  SELECT 1
                                  FROM tblTenantRecordGrant grantRecord
                                  WHERE grantRecord.TenantId = @TenantId
                                    AND grantRecord.TenantMembershipId = @MembershipId
                                    AND grantRecord.EntityType = 'Assessment'
                                    AND grantRecord.EntityId = assessment.AssessmentId
                                    AND grantRecord.Active = 1
                                    AND grantRecord.CanView = 1
                              )
                          )
                      )
                  )
            )
            SELECT DISTINCT
                advisor.UserId AS Id,
                COALESCE(
                    NULLIF(LTRIM(RTRIM(CONCAT(advisor.FirstName, ' ', advisor.LastName))), ''),
                    advisor.UserName,
                    advisor.Email,
                    'Unassigned') AS Name
            FROM AuthorisedAssessments authorised
            INNER JOIN tblApplicationUsers advisor ON advisor.UserId = authorised.CreatedBy
            WHERE advisor.IsActive = 1
            ORDER BY Name;
            """;

        using var connection = _dbConnectionFactory.CreateConnection();
        using var result = await connection.QueryMultipleAsync(new CommandDefinition(
            sql,
            ScopeParameters(context),
            cancellationToken: cancellationToken));

        var branches = (await result.ReadAsync<OperationalReportOption>()).AsList();
        var advisors = (await result.ReadAsync<OperationalReportOption>()).AsList();
        return new OperationalReportFilterOptions
        {
            Branches = branches,
            Advisors = advisors
        };
    }

    public async Task<IReadOnlyList<AssessmentOperationalReportRow>> GetAssessmentsAsync(
        AssessmentOperationalReportFilter filter,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(filter);
        await _tenantAuthorizationService.EnsureCanReadOperationalDataAsync(cancellationToken);
        var context = await _tenantAuthorizationService.GetAccessContextAsync(cancellationToken);

        var parameters = ScopeParameters(context);
        var predicates = new List<string>
        {
            "assessment.TenantId = @TenantId",
            """
            (
                @CanReadAll = 1
                OR
                (
                    @CanReadScoped = 1
                    AND
                    (
                        assessment.CreatedBy = @UserId
                        OR EXISTS
                        (
                            SELECT 1
                            FROM tblTenantRecordGrant grantRecord
                            WHERE grantRecord.TenantId = @TenantId
                              AND grantRecord.TenantMembershipId = @MembershipId
                              AND grantRecord.EntityType = 'Assessment'
                              AND grantRecord.EntityId = assessment.AssessmentId
                              AND grantRecord.Active = 1
                              AND grantRecord.CanView = 1
                        )
                    )
                )
            )
            """
        };

        if (filter.ActiveOnly)
            predicates.Add("assessment.Active = 1");
        if (filter.BranchId is > 0)
        {
            predicates.Add("advisor.BranchId = @BranchId");
            parameters.Add("BranchId", filter.BranchId);
        }
        if (filter.StatusId is > 0)
        {
            predicates.Add("assessment.StatusId = @StatusId");
            parameters.Add("StatusId", filter.StatusId);
        }
        if (filter.AdvisorId is > 0)
        {
            predicates.Add("assessment.CreatedBy = @AdvisorId");
            parameters.Add("AdvisorId", filter.AdvisorId);
        }
        if (filter.StartDateFrom.HasValue)
        {
            predicates.Add("assessment.AssessmentStartDate >= @StartDateFrom");
            parameters.Add("StartDateFrom", filter.StartDateFrom.Value.Date);
        }
        if (filter.StartDateTo.HasValue)
        {
            predicates.Add("assessment.AssessmentStartDate < DATEADD(DAY, 1, @StartDateTo)");
            parameters.Add("StartDateTo", filter.StartDateTo.Value.Date);
        }

        var searchText = filter.SearchText?.Trim();
        if (!string.IsNullOrWhiteSpace(searchText))
        {
            if (searchText.Length > 100)
                throw new ArgumentException("Search text cannot exceed 100 characters.", nameof(filter));

            predicates.Add("""
                (
                    assessment.CaseNumber LIKE @SearchPattern ESCAPE '\'
                    OR business.BusinessName LIKE @SearchPattern ESCAPE '\'
                    OR client.FullName LIKE @SearchPattern ESCAPE '\'
                    OR branch.BranchName LIKE @SearchPattern ESCAPE '\'
                    OR advisor.FirstName LIKE @SearchPattern ESCAPE '\'
                    OR advisor.LastName LIKE @SearchPattern ESCAPE '\'
                )
                """);
            parameters.Add("SearchPattern", $"%{EscapeLike(searchText)}%");
        }

        var sql = new StringBuilder("""
            SELECT
                assessment.AssessmentId,
                COALESCE(assessment.CaseNumber, CONCAT('Assessment-', assessment.AssessmentId)) AS CaseNumber,
                COALESCE(business.BusinessName, 'Unassigned') AS BusinessName,
                COALESCE(client.FullName, 'Unassigned') AS ClientName,
                advisor.BranchId,
                COALESCE(branch.BranchName, 'Unassigned') AS BranchName,
                assessment.CreatedBy AS AdvisorId,
                COALESCE(
                    NULLIF(LTRIM(RTRIM(CONCAT(advisor.FirstName, ' ', advisor.LastName))), ''),
                    advisor.UserName,
                    advisor.Email,
                    'Unassigned') AS AdvisorName,
                assessment.StatusId,
                CASE assessment.StatusId
                    WHEN 1 THEN 'Draft'
                    WHEN 2 THEN 'In Progress'
                    WHEN 3 THEN 'Ready for Review'
                    WHEN 4 THEN 'Completed'
                    WHEN 5 THEN 'Archived'
                    ELSE 'Unknown'
                END AS StatusName,
                assessment.ProgressPercentage,
                assessment.AssessmentStartDate,
                assessment.AssessmentFinishDate,
                assessment.CreatedDate,
                assessment.ModifiedDate,
                DATEDIFF(
                    DAY,
                    assessment.CreatedDate,
                    CASE
                        WHEN assessment.StatusId = 4 THEN assessment.ModifiedDate
                        ELSE SYSUTCDATETIME()
                    END) AS DaysOpen,
                assessment.Active
            FROM tblAssessments assessment
            LEFT JOIN tblBusiness business ON business.BusinessId = assessment.BusinessId
            LEFT JOIN tblClient client ON client.ClientId = business.ClientId
            LEFT JOIN tblApplicationUsers advisor ON advisor.UserId = assessment.CreatedBy
            LEFT JOIN tblBranch branch ON branch.BranchId = advisor.BranchId
            """);
        sql.AppendLine();
        sql.AppendLine("WHERE");
        sql.AppendLine(string.Join($"{Environment.NewLine}AND ", predicates));
        sql.AppendLine("ORDER BY assessment.AssessmentStartDate DESC, assessment.CaseNumber;");

        try
        {
            using var connection = _dbConnectionFactory.CreateConnection();
            return (await connection.QueryAsync<AssessmentOperationalReportRow>(
                new CommandDefinition(
                    sql.ToString(),
                    parameters,
                    cancellationToken: cancellationToken))).AsList();
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Operational assessment report failed for tenant {TenantId} and user {UserId}.",
                context.TenantId,
                context.UserId);
            throw;
        }
    }

    private static DynamicParameters ScopeParameters(
        ViabilityIQ.Shared.DataModels.SecurityDataModels.TenantAccessContext context)
    {
        var parameters = new DynamicParameters();
        parameters.Add("TenantId", context.TenantId);
        parameters.Add("UserId", context.UserId);
        parameters.Add("MembershipId", context.MembershipId);
        parameters.Add("CanReadAll", context.CanReadAllOperationalRecords);
        parameters.Add("CanReadScoped", context.CanReadScopedOperationalRecords);
        return parameters;
    }

    private static string EscapeLike(string value) =>
        value.Replace(@"\", @"\\", StringComparison.Ordinal)
            .Replace("%", @"\%", StringComparison.Ordinal)
            .Replace("_", @"\_", StringComparison.Ordinal)
            .Replace("[", @"\[", StringComparison.Ordinal);
}
