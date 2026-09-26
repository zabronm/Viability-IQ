using System.Data;
using System.Text.RegularExpressions;
using Dapper;
using ViabilityIQ.Application.Dtos;
using ViabilityIQ.Application.Interfaces;
using ViabilityIQ.Infrastructure.DbFactory;
using ViabilityIQ.Shared.DataModels.SecurityDataModels;

namespace ViabilityIQ.Infrastructure.Repositories;

public sealed class TenantService : ITenantService
{
    private static readonly string[] DefaultRoleCodes =
    [
        TenantRoleCodes.Owner,
        TenantRoleCodes.Administrator,
        TenantRoleCodes.BranchManager,
        TenantRoleCodes.Viewer,
        TenantRoleCodes.BillingManager
    ];

    private readonly IDbConnectionFactory _dbConnectionFactory;

    public TenantService(IDbConnectionFactory dbConnectionFactory)
    {
        _dbConnectionFactory = dbConnectionFactory;
    }

    public async Task<TenantProvisioningResult> ProvisionTenantAsync(
        TenantProvisioningRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var planCode = request.PlanCode.Trim().ToUpperInvariant();
        if (planCode is not (SubscriptionPlanCodes.Standard or SubscriptionPlanCodes.Open))
        {
            return Failed("The selected subscription plan is not supported.");
        }

        var isOpenPlan = planCode == SubscriptionPlanCodes.Open;
        var tenantName = isOpenPlan
            ? request.OrganisationName.Trim()
            : $"{request.FirstName} {request.LastName}".Trim();

        if (string.IsNullOrWhiteSpace(tenantName))
        {
            return Failed(isOpenPlan
                ? "An organisation name is required for Open Plan."
                : "A tenant name could not be determined.");
        }

        var requestedSeats = isOpenPlan ? Math.Max(2, request.RequestedSeats) : 1;
        using var connection = _dbConnectionFactory.CreateConnection();
        if (connection.State != ConnectionState.Open)
        {
            connection.Open();
        }

        using var transaction = connection.BeginTransaction();
        try
        {
            var command = new CommandDefinition(
                """
                SELECT TOP (1)
                    SubscriptionPlanId,
                    IncludedSeats,
                    AllowsAdditionalSeats,
                    TrialDays
                FROM tblSubscriptionPlan
                WHERE PlanCode = @PlanCode AND Active = 1;
                """,
                new { PlanCode = planCode },
                transaction,
                cancellationToken: cancellationToken);
            var plan = await connection.QuerySingleOrDefaultAsync<PlanRecord>(command);
            if (plan is null)
            {
                transaction.Rollback();
                return Failed("The selected subscription plan is unavailable.");
            }

            var seatQuantity = plan.AllowsAdditionalSeats
                ? Math.Max(plan.IncludedSeats, requestedSeats)
                : plan.IncludedSeats;
            var slug = await CreateUniqueSlugAsync(connection, transaction, tenantName, cancellationToken);
            var now = DateTime.UtcNow;

            var tenantId = await connection.ExecuteScalarAsync<long>(
                """
                INSERT INTO tblTenant
                    (TenantType, TenantName, TenantSlug, OwnerUserId, ProvinceId,
                     Status, Active, CreatedDate, CreatedBy)
                OUTPUT INSERTED.TenantId
                VALUES
                    (@TenantType, @TenantName, @TenantSlug, @OwnerUserId, @ProvinceId,
                     @Status, 1, @CreatedDate, @CreatedBy);
                """,
                new
                {
                    TenantType = isOpenPlan ? TenantTypes.Organisation : TenantTypes.Personal,
                    TenantName = tenantName,
                    TenantSlug = slug,
                    OwnerUserId = request.UserId,
                    request.ProvinceId,
                    Status = TenantSubscriptionStatuses.Active,
                    CreatedDate = now,
                    CreatedBy = request.UserId
                },
                transaction);

            var membershipId = await connection.ExecuteScalarAsync<long>(
                """
                INSERT INTO tblTenantMembership
                    (TenantId, UserId, MembershipStatus, IsOwner, JoinedDate, Active)
                OUTPUT INSERTED.TenantMembershipId
                VALUES
                    (@TenantId, @UserId, @Status, 1, @JoinedDate, 1);
                """,
                new
                {
                    TenantId = tenantId,
                    UserId = request.UserId,
                    Status = TenantMembershipStatuses.Active,
                    JoinedDate = now
                },
                transaction);

            long ownerRoleId = 0;
            foreach (var roleCode in DefaultRoleCodes)
            {
                var roleId = await connection.ExecuteScalarAsync<long>(
                    """
                    INSERT INTO tblTenantRole
                        (TenantId, RoleCode, RoleName, Description, IsProtected, Active, CreatedDate)
                    OUTPUT INSERTED.TenantRoleId
                    VALUES
                        (@TenantId, @RoleCode, @RoleName, @Description, 1, 1, @CreatedDate);
                    """,
                    new
                    {
                        TenantId = tenantId,
                        RoleCode = roleCode,
                        RoleName = GetRoleName(roleCode),
                        Description = GetRoleDescription(roleCode),
                        CreatedDate = now
                    },
                    transaction);

                if (roleCode == TenantRoleCodes.Owner)
                {
                    ownerRoleId = roleId;
                }
            }

            await connection.ExecuteAsync(
                """
                INSERT INTO tblTenantMembershipRole
                    (TenantMembershipId, TenantRoleId, AssignedDate, AssignedBy)
                VALUES
                    (@TenantMembershipId, @TenantRoleId, @AssignedDate, @AssignedBy);
                """,
                new
                {
                    TenantMembershipId = membershipId,
                    TenantRoleId = ownerRoleId,
                    AssignedDate = now,
                    AssignedBy = request.UserId
                },
                transaction);

            await connection.ExecuteAsync(
                """
                INSERT INTO tblTenantSubscription
                    (TenantId, SubscriptionPlanId, Status, SeatQuantity,
                     CurrentPeriodStart, TrialEndsAt, CancelAtPeriodEnd, Active)
                VALUES
                    (@TenantId, @SubscriptionPlanId, @Status, @SeatQuantity,
                     @CurrentPeriodStart, @TrialEndsAt, 0, 1);
                """,
                new
                {
                    TenantId = tenantId,
                    plan.SubscriptionPlanId,
                    Status = TenantSubscriptionStatuses.Active,
                    SeatQuantity = seatQuantity,
                    CurrentPeriodStart = now,
                    TrialEndsAt = (DateTime?)null
                },
                transaction);

            transaction.Commit();
            return new TenantProvisioningResult { Success = true, TenantId = tenantId };
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public async Task<TenantContextDto?> GetDefaultTenantAsync(
        long userId,
        CancellationToken cancellationToken = default)
    {
        var tenants = await GetUserTenantsAsync(userId, cancellationToken);
        return tenants.FirstOrDefault();
    }

    public async Task<IReadOnlyList<TenantContextDto>> GetUserTenantsAsync(
        long userId,
        CancellationToken cancellationToken = default)
    {
        using var connection = _dbConnectionFactory.CreateConnection();
        var command = new CommandDefinition(
            """
            SELECT
                t.TenantId,
                t.TenantName,
                t.TenantType,
                p.PlanCode,
                s.Status AS SubscriptionStatus,
                s.SeatQuantity,
                m.TenantMembershipId AS MembershipId,
                m.IsOwner
            FROM tblTenantMembership m
            INNER JOIN tblTenant t ON t.TenantId = m.TenantId
            INNER JOIN tblTenantSubscription s
                ON s.TenantId = t.TenantId AND s.Active = 1
            INNER JOIN tblSubscriptionPlan p
                ON p.SubscriptionPlanId = s.SubscriptionPlanId
            WHERE m.UserId = @UserId
              AND m.Active = 1
              AND m.MembershipStatus = @MembershipStatus
              AND t.Active = 1
            ORDER BY m.IsOwner DESC, t.TenantName;
            """,
            new
            {
                UserId = userId,
                MembershipStatus = TenantMembershipStatuses.Active
            },
            cancellationToken: cancellationToken);

        var results = await connection.QueryAsync<TenantContextDto>(command);
        return results.AsList();
    }

    private static TenantProvisioningResult Failed(string message) =>
        new() { Success = false, ErrorMessage = message };

    private static string GetRoleName(string roleCode) => roleCode switch
    {
        TenantRoleCodes.Owner => "Tenant Owner",
        TenantRoleCodes.Administrator => "Tenant Administrator",
        TenantRoleCodes.BranchManager => "Branch Manager",
        TenantRoleCodes.BillingManager => "Billing Manager",
        _ => "Viewer"
    };

    private static string GetRoleDescription(string roleCode) => roleCode switch
    {
        TenantRoleCodes.Owner => "Owns the tenant and controls all tenant settings.",
        TenantRoleCodes.Administrator => "Manages tenant users, setup, and operational records.",
        TenantRoleCodes.BranchManager => "Manages a branch and performs assessment review responsibilities.",
        TenantRoleCodes.BillingManager => "Manages subscription and billing settings.",
        _ => "Views authorised tenant records."
    };

    private static async Task<string> CreateUniqueSlugAsync(
        IDbConnection connection,
        IDbTransaction transaction,
        string tenantName,
        CancellationToken cancellationToken)
    {
        var baseSlug = Regex.Replace(tenantName.Trim().ToLowerInvariant(), "[^a-z0-9]+", "-")
            .Trim('-');
        if (string.IsNullOrWhiteSpace(baseSlug))
        {
            baseSlug = "tenant";
        }

        for (var suffix = 0; suffix < 1000; suffix++)
        {
            var candidate = suffix == 0 ? baseSlug : $"{baseSlug}-{suffix + 1}";
            var command = new CommandDefinition(
                "SELECT COUNT(1) FROM tblTenant WHERE TenantSlug = @TenantSlug;",
                new { TenantSlug = candidate },
                transaction,
                cancellationToken: cancellationToken);
            if (await connection.ExecuteScalarAsync<int>(command) == 0)
            {
                return candidate;
            }
        }

        return $"{baseSlug}-{Guid.NewGuid():N}";
    }

    private sealed class PlanRecord
    {
        public long SubscriptionPlanId { get; init; }
        public int IncludedSeats { get; init; }
        public bool AllowsAdditionalSeats { get; init; }
        public int TrialDays { get; init; }
    }
}
