using Dapper;
using ViabilityIQ.Application.Interfaces;
using ViabilityIQ.Infrastructure.DbFactory;
using ViabilityIQ.Shared.DataModels.SecurityDataModels;

namespace ViabilityIQ.Infrastructure.Repositories;

public sealed class TenantAuthorizationService : ITenantAuthorizationService
{
    private readonly IDbConnectionFactory _dbConnectionFactory;
    private readonly ISessionService _sessionService;

    public TenantAuthorizationService(
        IDbConnectionFactory dbConnectionFactory,
        ISessionService sessionService)
    {
        _dbConnectionFactory = dbConnectionFactory;
        _sessionService = sessionService;
    }

    public Task<TenantAccessContext> GetAccessContextAsync(
        CancellationToken cancellationToken = default) =>
        LoadAccessContextAsync(cancellationToken);

    public async Task EnsureCanReadOperationalDataAsync(
        CancellationToken cancellationToken = default)
    {
        var context = await GetAccessContextAsync(cancellationToken);
        if (!context.CanReadAllOperationalRecords
            && !context.CanReadScopedOperationalRecords)
        {
            throw new UnauthorizedAccessException(
                "Your tenant role does not permit access to operational records.");
        }
    }

    public async Task EnsureCanCreateOperationalDataAsync(
        CancellationToken cancellationToken = default)
    {
        var context = await GetAccessContextAsync(cancellationToken);
        if (!context.CanWriteAllOperationalRecords
            && !context.CanWriteScopedOperationalRecords)
        {
            throw new UnauthorizedAccessException(
                "Your tenant role does not permit changes to operational records.");
        }
    }

    public async Task EnsureCanAccessAssessmentAsync(
        long assessmentId,
        TenantRecordAccess access,
        CancellationToken cancellationToken = default)
    {
        if (assessmentId <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(assessmentId),
                "A valid assessment identifier is required.");
        }

        var context = await GetAccessContextAsync(cancellationToken);
        var canAccessAll = access == TenantRecordAccess.Read
            ? context.CanReadAllOperationalRecords
            : context.CanWriteAllOperationalRecords;
        var canAccessScoped = access switch
        {
            TenantRecordAccess.Read => context.CanReadScopedOperationalRecords,
            TenantRecordAccess.Write => context.CanWriteScopedOperationalRecords,
            TenantRecordAccess.Delete => context.CanDeleteScopedOperationalRecords,
            _ => false
        };
        var grantColumn = access switch
        {
            TenantRecordAccess.Read => "CanView",
            TenantRecordAccess.Write => "CanEdit",
            TenantRecordAccess.Delete => "CanDelete",
            _ => throw new ArgumentOutOfRangeException(nameof(access))
        };

        using var connection = _dbConnectionFactory.CreateConnection();
        var sql = $"""
            SELECT COUNT_BIG(1)
            FROM tblAssessments assessment
            WHERE assessment.AssessmentId = @AssessmentId
              AND assessment.TenantId = @TenantId
              AND
              (
                  @CanAccessAll = 1
                  OR (@CanAccessScoped = 1 AND assessment.CreatedBy = @UserId)
                  OR EXISTS
                  (
                      SELECT 1
                      FROM tblTenantRecordGrant grantRecord
                      WHERE grantRecord.TenantId = @TenantId
                        AND grantRecord.TenantMembershipId = @MembershipId
                        AND grantRecord.EntityType = 'Assessment'
                        AND grantRecord.EntityId = assessment.AssessmentId
                        AND grantRecord.Active = 1
                        AND grantRecord.[{grantColumn}] = 1
                  )
              );
            """;
        var count = await connection.ExecuteScalarAsync<long>(
            new CommandDefinition(
                sql,
                new
                {
                    AssessmentId = assessmentId,
                    context.TenantId,
                    context.UserId,
                    context.MembershipId,
                    CanAccessAll = canAccessAll,
                    CanAccessScoped = canAccessScoped
                },
                cancellationToken: cancellationToken));
        if (count == 0)
        {
            throw new UnauthorizedAccessException(
                "The selected assessment is outside your authorised scope.");
        }
    }

    private async Task<TenantAccessContext> LoadAccessContextAsync(
        CancellationToken cancellationToken)
    {
        if (!_sessionService.IsAuthenticated
            || _sessionService.UserId <= 0
            || _sessionService.TenantId <= 0
            || _sessionService.TenantMembershipId <= 0)
        {
            throw new UnauthorizedAccessException(
                "An authenticated user and active tenant are required.");
        }

        using var connection = _dbConnectionFactory.CreateConnection();
        var command = new CommandDefinition(
            """
            SELECT
                m.TenantMembershipId,
                m.IsOwner,
                r.RoleCode
            FROM tblTenantMembership m
            INNER JOIN tblTenant t
                ON t.TenantId = m.TenantId
               AND t.Active = 1
            INNER JOIN tblTenantSubscription s
                ON s.TenantId = t.TenantId
               AND s.Active = 1
               AND s.Status IN ('Active', 'Trial')
            LEFT JOIN tblTenantMembershipRole mr
                ON mr.TenantMembershipId = m.TenantMembershipId
            LEFT JOIN tblTenantRole r
                ON r.TenantRoleId = mr.TenantRoleId
               AND r.TenantId = m.TenantId
               AND r.Active = 1
            WHERE m.TenantMembershipId = @MembershipId
              AND m.TenantId = @TenantId
              AND m.UserId = @UserId
              AND m.Active = 1
              AND m.MembershipStatus = 'Active';
            """,
            new
            {
                MembershipId = _sessionService.TenantMembershipId,
                TenantId = _sessionService.TenantId,
                UserId = _sessionService.UserId
            },
            cancellationToken: cancellationToken);

        var rows = (await connection.QueryAsync<MembershipRoleRow>(command)).AsList();
        if (rows.Count == 0)
        {
            throw new UnauthorizedAccessException(
                "The active tenant membership or subscription is unavailable.");
        }

        var roleCodes = rows
            .Where(row => !string.IsNullOrWhiteSpace(row.RoleCode))
            .Select(row => row.RoleCode!)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var isOwner = rows.Any(row => row.IsOwner)
            || roleCodes.Contains(TenantRoleCodes.Owner);
        var isAdministrator = roleCodes.Contains(TenantRoleCodes.Administrator);
        var isBranchManager = roleCodes.Contains(TenantRoleCodes.BranchManager);
        var isViewer = roleCodes.Contains(TenantRoleCodes.Viewer);

        return new TenantAccessContext
        {
            TenantId = _sessionService.TenantId,
            UserId = _sessionService.UserId,
            MembershipId = _sessionService.TenantMembershipId,
            RoleCodes = roleCodes,
            CanReadAllOperationalRecords = isOwner || isAdministrator,
            CanWriteAllOperationalRecords = isOwner || isAdministrator,
            CanReadScopedOperationalRecords =
                isOwner || isAdministrator || isBranchManager || isViewer,
            CanWriteScopedOperationalRecords =
                isOwner || isAdministrator || isBranchManager,
            CanDeleteScopedOperationalRecords =
                isOwner || isAdministrator || isBranchManager
        };
    }

    private sealed class MembershipRoleRow
    {
        public long TenantMembershipId { get; init; }
        public bool IsOwner { get; init; }
        public string? RoleCode { get; init; }
    }
}
