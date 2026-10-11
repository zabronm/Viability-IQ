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
        var tenantName = request.OrganisationName.Trim();

        if (string.IsNullOrWhiteSpace(tenantName))
        {
            return Failed("A Company / Organisation name is required.");
        }

        var regionName = request.InitialRegionName.Trim();
        var siteName = request.InitialSiteName.Trim();
        if (string.IsNullOrWhiteSpace(regionName)
            || string.IsNullOrWhiteSpace(siteName))
        {
            return Failed("An initial Region and Site are required.");
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
                    TenantType = TenantTypes.Organisation,
                    TenantName = tenantName,
                    TenantSlug = slug,
                    OwnerUserId = request.UserId,
                    ProvinceId = (long?)null,
                    Status = TenantSubscriptionStatuses.Active,
                    CreatedDate = now,
                    CreatedBy = request.UserId
                },
                transaction);

            var regionId = await connection.ExecuteScalarAsync<long>(
                """
                INSERT INTO tblProvince
                    (TenantId, ProvinceName, ShortName, Manager_UserId,
                     Active, CreatedDate, CreatedBy, ModifiedDate, ModifiedBy)
                OUTPUT INSERTED.ProvinceId
                VALUES
                    (@TenantId, @ProvinceName, @ShortName, @ManagerUserId,
                     1, @CreatedDate, @CreatedBy, @ModifiedDate, @ModifiedBy);
                """,
                new
                {
                    TenantId = tenantId,
                    ProvinceName = regionName,
                    ShortName = regionName.Length <= 12
                        ? regionName
                        : regionName[..12],
                    ManagerUserId = request.UserId,
                    CreatedDate = now,
                    CreatedBy = request.UserId,
                    ModifiedDate = now,
                    ModifiedBy = request.UserId
                },
                transaction);

            var companyId = await connection.ExecuteScalarAsync<long>(
                """
                INSERT INTO tblCompany
                    (TenantId, CompanyName, CKNumber,
                     ContactPerson, Street_Address, Suburb, CityTown,
                     ProvinceId, Country, PostalCode, Telephone, Mobile, Email, Website, Active,
                     CreatedDate, CreatedBy, ModifiedDate, ModifiedBy)
                OUTPUT INSERTED.CompanyId
                VALUES
                    (@TenantId, @CompanyName, @CKNumber,
                     @ContactPerson, @StreetAddress, @Suburb, @CityTown,
                     @ProvinceId, @Country, @PostalCode, @Telephone, @Mobile, @Email, @Website, 1,
                     @CreatedDate, @CreatedBy, @ModifiedDate, @ModifiedBy);
                """,
                new
                {
                    TenantId = tenantId,
                    CompanyName = tenantName,
                    CKNumber = request.CompanyRegistrationNumber,
                    ContactPerson = string.IsNullOrWhiteSpace(request.CompanyContactPerson)
                        ? $"{request.FirstName} {request.LastName}".Trim()
                        : request.CompanyContactPerson.Trim(),
                    StreetAddress = request.CompanyStreetAddress,
                    Suburb = request.CompanySuburb,
                    CityTown = request.CompanyCityTown,
                    ProvinceId = regionId,
                    Country = request.CompanyCountry,
                    PostalCode = request.CompanyPostalCode,
                    Telephone = request.CompanyTelephone,
                    Mobile = request.CompanyMobile,
                    Email = request.CompanyEmail,
                    Website = request.CompanyWebsite,
                    CreatedDate = now,
                    CreatedBy = request.UserId,
                    ModifiedDate = now,
                    ModifiedBy = request.UserId
                },
                transaction);

            var siteCode = await CreateUniqueSiteCodeAsync(
                connection,
                transaction,
                tenantId,
                siteName,
                cancellationToken);
            var siteId = await connection.ExecuteScalarAsync<long>(
                """
                INSERT INTO tblBranch
                    (TenantId, BranchCode, BranchName, ProvinceId,
                     Manager_UserId, Active, CreatedDate, CreatedBy,
                     ModifiedDate, ModifiedBy)
                OUTPUT INSERTED.BranchId
                VALUES
                    (@TenantId, @BranchCode, @BranchName, @ProvinceId,
                     @ManagerUserId, 1, @CreatedDate, @CreatedBy,
                     @ModifiedDate, @ModifiedBy);
                """,
                new
                {
                    TenantId = tenantId,
                    BranchCode = siteCode,
                    BranchName = siteName,
                    ProvinceId = regionId,
                    ManagerUserId = request.UserId,
                    CreatedDate = now,
                    CreatedBy = request.UserId,
                    ModifiedDate = now,
                    ModifiedBy = request.UserId
                },
                transaction);

            await connection.ExecuteAsync(
                """
                UPDATE tblTenant
                SET ProvinceId = @ProvinceId
                WHERE TenantId = @TenantId;
                """,
                new { ProvinceId = regionId, TenantId = tenantId },
                transaction);

            var membershipId = await connection.ExecuteScalarAsync<long>(
                """
                INSERT INTO tblTenantMembership
                    (TenantId, UserId, MembershipStatus, IsOwner,
                     DefaultBranchId, JoinedDate, Active)
                OUTPUT INSERTED.TenantMembershipId
                VALUES
                    (@TenantId, @UserId, @Status, 1,
                     @DefaultBranchId, @JoinedDate, 1);
                """,
                new
                {
                    TenantId = tenantId,
                    UserId = request.UserId,
                    Status = TenantMembershipStatuses.Active,
                    DefaultBranchId = siteId,
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
            return new TenantProvisioningResult
            {
                Success = true,
                TenantId = tenantId,
                CompanyId = companyId,
                RegionId = regionId,
                SiteId = siteId
            };
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
                m.DefaultBranchId AS BranchId,
                COALESCE(branch.ProvinceId, 0) AS ProvinceId,
                m.IsOwner
            FROM tblTenantMembership m
            INNER JOIN tblTenant t ON t.TenantId = m.TenantId
            INNER JOIN tblTenantSubscription s
                ON s.TenantId = t.TenantId AND s.Active = 1
            INNER JOIN tblSubscriptionPlan p
                ON p.SubscriptionPlanId = s.SubscriptionPlanId
            LEFT JOIN tblBranch branch
                ON branch.BranchId = m.DefaultBranchId
               AND branch.TenantId = m.TenantId
               AND branch.Active = 1
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

    private static async Task<string> CreateUniqueSiteCodeAsync(
        IDbConnection connection,
        IDbTransaction transaction,
        long tenantId,
        string siteName,
        CancellationToken cancellationToken)
    {
        var candidates = OperationalReferenceService.BuildThreeLetterCandidates(siteName);
        foreach (var candidate in candidates)
        {
            if (!await SiteCodeExistsAsync(
                    connection,
                    transaction,
                    tenantId,
                    candidate,
                    cancellationToken))
            {
                return candidate;
            }
        }

        var baseCode = candidates[0];
        for (var suffix = 0; suffix < int.MaxValue; suffix++)
        {
            var suffixText = suffix.ToString();
            var candidate =
                $"{baseCode[..Math.Min(baseCode.Length, 12 - suffixText.Length)]}{suffixText}";
            if (!await SiteCodeExistsAsync(
                    connection,
                    transaction,
                    tenantId,
                    candidate,
                    cancellationToken))
            {
                return candidate;
            }
        }

        throw new InvalidOperationException("No available Site Code could be generated.");
    }

    private static async Task<bool> SiteCodeExistsAsync(
        IDbConnection connection,
        IDbTransaction transaction,
        long tenantId,
        string siteCode,
        CancellationToken cancellationToken) =>
        await connection.ExecuteScalarAsync<long>(
            new CommandDefinition(
                """
                SELECT COUNT_BIG(1)
                FROM tblBranch WITH (UPDLOCK, HOLDLOCK)
                WHERE TenantId = @TenantId
                  AND BranchCode = @BranchCode;
                """,
                new { TenantId = tenantId, BranchCode = siteCode },
                transaction,
                cancellationToken: cancellationToken)) > 0;

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
