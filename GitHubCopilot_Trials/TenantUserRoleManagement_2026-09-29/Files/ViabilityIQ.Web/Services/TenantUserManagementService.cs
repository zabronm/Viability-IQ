using System.Data;
using System.Net;
using System.Security.Cryptography;
using Dapper;
using Microsoft.AspNetCore.Identity;
using ViabilityIQ.Application.Dtos;
using ViabilityIQ.Application.Interfaces;
using ViabilityIQ.Infrastructure.DbFactory;
using ViabilityIQ.Shared.DataModels.SecurityDataModels;
using ViabilityIQ.Shared.SharedModels;

namespace ViabilityIQ.Web.Services;

public sealed class TenantUserManagementService : ITenantUserManagementService
{
    private static readonly HashSet<string> AssignableRoleCodes =
    [
        TenantRoleCodes.Administrator,
        TenantRoleCodes.BranchManager,
        TenantRoleCodes.Viewer,
        TenantRoleCodes.BillingManager
    ];

    private readonly IDbConnectionFactory _dbConnectionFactory;
    private readonly ITenantAuthorizationService _tenantAuthorizationService;
    private readonly ISessionService _sessionService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly IEmailReportingService _emailService;
    private readonly ILogger<TenantUserManagementService> _logger;

    public TenantUserManagementService(
        IDbConnectionFactory dbConnectionFactory,
        ITenantAuthorizationService tenantAuthorizationService,
        ISessionService sessionService,
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager,
        IEmailReportingService emailService,
        ILogger<TenantUserManagementService> logger)
    {
        _dbConnectionFactory = dbConnectionFactory;
        _tenantAuthorizationService = tenantAuthorizationService;
        _sessionService = sessionService;
        _userManager = userManager;
        _roleManager = roleManager;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task<TenantUserManagementSnapshot> GetSnapshotAsync(
        CancellationToken cancellationToken = default)
    {
        var context = await RequireManagerAsync(cancellationToken);
        const string sql = """
            SELECT
                tenant.TenantName,
                subscription.SeatQuantity,
                (
                    SELECT COUNT(1)
                    FROM tblTenantMembership activeMembership
                    WHERE activeMembership.TenantId = @TenantId
                      AND activeMembership.Active = 1
                      AND activeMembership.MembershipStatus = 'Active'
                ) AS ActiveMemberCount
            FROM tblTenant tenant
            INNER JOIN tblTenantSubscription subscription
                ON subscription.TenantId = tenant.TenantId
               AND subscription.Active = 1
            WHERE tenant.TenantId = @TenantId;

            SELECT
                membership.TenantMembershipId AS MembershipId,
                membership.UserId,
                COALESCE(
                    NULLIF(LTRIM(RTRIM(CONCAT(appUser.FirstName, ' ', appUser.LastName))), ''),
                    appUser.UserName,
                    appUser.Email) AS FullName,
                appUser.Email,
                membership.MembershipStatus,
                membership.IsOwner,
                membership.DefaultBranchId AS BranchId,
                COALESCE(branch.BranchName, 'Unassigned') AS BranchName,
                appUser.LastLoginAt,
                appUser.MustChangePassword
            FROM tblTenantMembership membership
            INNER JOIN tblApplicationUsers appUser ON appUser.UserId = membership.UserId
            LEFT JOIN tblBranch branch
                ON branch.BranchId = membership.DefaultBranchId
               AND branch.TenantId = membership.TenantId
            WHERE membership.TenantId = @TenantId
              AND membership.MembershipStatus <> 'Removed'
            ORDER BY membership.IsOwner DESC, FullName;

            SELECT
                role.TenantRoleId AS RoleId,
                role.RoleCode,
                role.RoleName,
                role.Description,
                role.IsProtected
            FROM tblTenantRole role
            WHERE role.TenantId = @TenantId
              AND role.Active = 1
            ORDER BY
                CASE role.RoleCode
                    WHEN 'TENANT_OWNER' THEN 1
                    WHEN 'TENANT_ADMIN' THEN 2
                    WHEN 'BRANCH_MANAGER' THEN 3
                    WHEN 'VIEWER' THEN 4
                    WHEN 'BILLING_MANAGER' THEN 5
                    ELSE 6
                END;

            SELECT
                branch.BranchId,
                branch.BranchName
            FROM tblBranch branch
            WHERE branch.TenantId = @TenantId
              AND branch.Active = 1
            ORDER BY branch.BranchName;

            SELECT
                membershipRole.TenantMembershipId AS MembershipId,
                role.RoleCode,
                role.RoleName
            FROM tblTenantMembershipRole membershipRole
            INNER JOIN tblTenantMembership membership
                ON membership.TenantMembershipId = membershipRole.TenantMembershipId
            INNER JOIN tblTenantRole role
                ON role.TenantRoleId = membershipRole.TenantRoleId
            WHERE membership.TenantId = @TenantId
              AND role.Active = 1;
            """;

        using var connection = _dbConnectionFactory.CreateConnection();
        using var results = await connection.QueryMultipleAsync(
            new CommandDefinition(sql, new { context.TenantId }, cancellationToken: cancellationToken));

        var header = await results.ReadSingleAsync<TenantHeader>();
        var members = (await results.ReadAsync<TenantMemberRow>()).AsList();
        var roles = (await results.ReadAsync<TenantRoleOption>()).AsList();
        var branches = (await results.ReadAsync<TenantBranchOption>()).AsList();
        var assignments = (await results.ReadAsync<RoleAssignment>()).AsList();

        foreach (var member in members)
        {
            member.RoleCodes = assignments
                .Where(item => item.MembershipId == member.MembershipId)
                .Select(item => item.RoleCode)
                .ToArray();
            member.RoleNames = assignments
                .Where(item => item.MembershipId == member.MembershipId)
                .Select(item => item.RoleName)
                .ToArray();
        }

        return new TenantUserManagementSnapshot
        {
            CanManageUsers = true,
            TenantName = header.TenantName,
            SeatQuantity = header.SeatQuantity,
            ActiveMemberCount = header.ActiveMemberCount,
            Members = members,
            Roles = roles,
            Branches = branches
        };
    }

    public async Task<TenantUserOperationResult> CreateMemberAsync(
        CreateTenantUserRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var context = await RequireManagerAsync(cancellationToken);
        var roleCodes = NormalizeRoles(request.RoleCodes);
        if (roleCodes.Count == 0)
            return Failed("Select at least one tenant role.");

        var email = request.Email.Trim();
        if (string.IsNullOrWhiteSpace(request.FirstName)
            || string.IsNullOrWhiteSpace(request.LastName)
            || !new System.ComponentModel.DataAnnotations.EmailAddressAttribute().IsValid(email))
        {
            return Failed("First name, last name, and a valid email address are required.");
        }

        using var connection = _dbConnectionFactory.CreateConnection();
        await OpenAsync(connection, cancellationToken);
        if (!await HasSeatAvailableAsync(connection, context.TenantId, cancellationToken))
            return Failed("The tenant has reached its subscribed user-seat limit.");
        if (!await BranchIsValidAsync(connection, context.TenantId, request.BranchId, cancellationToken))
            return Failed("The selected branch is not active in this tenant.");

        var existingUser = await _userManager.FindByEmailAsync(email);
        var existingMembership = existingUser is null
            ? null
            : await GetExistingMembershipAsync(
                connection,
                context.TenantId,
                existingUser.Id,
                cancellationToken);
        if (existingMembership is not null
            && existingMembership.MembershipStatus != TenantMembershipStatuses.Removed)
        {
            return Failed("This user is already a member of the active tenant.");
        }

        ApplicationUser user;
        string? temporaryPassword = null;
        var createdAccount = existingUser is null;
        if (createdAccount)
        {
            temporaryPassword = GenerateTemporaryPassword();
            user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                FirstName = request.FirstName.Trim(),
                LastName = request.LastName.Trim(),
                BranchId = request.BranchId,
                CreatedAt = DateTime.UtcNow,
                IsActive = true,
                MustChangePassword = true,
                EmailConfirmed = true
            };

            var createResult = await _userManager.CreateAsync(user, temporaryPassword);
            if (!createResult.Succeeded)
                return Failed(string.Join(" ", createResult.Errors.Select(error => error.Description)));

            if (await _roleManager.RoleExistsAsync("User"))
            {
                var identityRoleResult = await _userManager.AddToRoleAsync(user, "User");
                if (!identityRoleResult.Succeeded)
                {
                    await _userManager.DeleteAsync(user);
                    return Failed("The account could not be assigned its base application role.");
                }
            }
        }
        else
        {
            user = existingUser!;
        }

        long membershipId;
        using (var transaction = connection.BeginTransaction())
        {
            try
            {
                if (existingMembership is null)
                {
                    membershipId = await connection.ExecuteScalarAsync<long>(
                        new CommandDefinition(
                            """
                            INSERT INTO tblTenantMembership
                                (TenantId, UserId, MembershipStatus, IsOwner,
                                 DefaultBranchId, JoinedDate, Active)
                            OUTPUT INSERTED.TenantMembershipId
                            VALUES
                                (@TenantId, @UserId, 'Active', 0,
                                 @BranchId, SYSUTCDATETIME(), 1);
                            """,
                            new
                            {
                                context.TenantId,
                                UserId = user.Id,
                                BranchId = request.BranchId
                            },
                            transaction,
                            cancellationToken: cancellationToken));
                }
                else
                {
                    membershipId = existingMembership.MembershipId;
                    await connection.ExecuteAsync(
                        new CommandDefinition(
                            """
                            UPDATE tblTenantMembership
                            SET MembershipStatus = 'Active',
                                DefaultBranchId = @BranchId,
                                JoinedDate = SYSUTCDATETIME(),
                                Active = 1
                            WHERE TenantMembershipId = @MembershipId
                              AND TenantId = @TenantId;

                            DELETE FROM tblTenantMembershipRole
                            WHERE TenantMembershipId = @MembershipId;
                            """,
                            new
                            {
                                MembershipId = membershipId,
                                context.TenantId,
                                BranchId = request.BranchId
                            },
                            transaction,
                            cancellationToken: cancellationToken));
                }

                await InsertRolesAsync(
                    connection,
                    transaction,
                    context.TenantId,
                    membershipId,
                    context.UserId,
                    roleCodes,
                    cancellationToken);
                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                if (createdAccount)
                    await _userManager.DeleteAsync(user);
                throw;
            }
        }

        var emailResult = await SendWelcomeEmailAsync(
            user,
            temporaryPassword,
            createdAccount,
            cancellationToken);
        if (!emailResult.Succeeded)
        {
            await RemoveMembershipAsync(connection, membershipId, cancellationToken);
            if (createdAccount)
                await _userManager.DeleteAsync(user);

            _logger.LogError(
                "Tenant user provisioning was rolled back because welcome email delivery failed. Reference {Reference}",
                emailResult.Reference);
            return Failed(
                $"The account was not added because the welcome email could not be delivered. Reference: {emailResult.Reference}");
        }

        return new TenantUserOperationResult(
            true,
            createdAccount
                ? "User created and temporary sign-in details emailed successfully."
                : "Existing account added to the tenant and notification emailed successfully.",
            !createdAccount);
    }

    public async Task<TenantUserOperationResult> UpdateMemberAsync(
        UpdateTenantMemberRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var context = await RequireManagerAsync(cancellationToken);
        var roleCodes = NormalizeRoles(request.RoleCodes);
        if (roleCodes.Count == 0)
            return Failed("Select at least one tenant role.");
        if (request.MembershipStatus is not ("Active" or "Suspended" or "Removed"))
            return Failed("The selected membership status is invalid.");

        using var connection = _dbConnectionFactory.CreateConnection();
        await OpenAsync(connection, cancellationToken);
        if (!await BranchIsValidAsync(connection, context.TenantId, request.BranchId, cancellationToken))
            return Failed("The selected branch is not active in this tenant.");

        var target = await connection.QuerySingleOrDefaultAsync<MemberTarget>(
            new CommandDefinition(
                """
                SELECT TenantMembershipId, UserId, IsOwner, MembershipStatus
                FROM tblTenantMembership
                WHERE TenantMembershipId = @MembershipId
                  AND TenantId = @TenantId;
                """,
                new { request.MembershipId, context.TenantId },
                cancellationToken: cancellationToken));
        if (target is null)
            return Failed("The selected tenant member could not be found.");
        if (target.IsOwner)
            return Failed("The Tenant Owner role and membership cannot be changed here.");
        if (target.UserId == context.UserId && request.MembershipStatus != "Active")
            return Failed("You cannot suspend or remove your own active membership.");
        if (target.MembershipStatus != "Active"
            && request.MembershipStatus == "Active"
            && !await HasSeatAvailableAsync(connection, context.TenantId, cancellationToken))
        {
            return Failed("The tenant has reached its subscribed user-seat limit.");
        }

        using var transaction = connection.BeginTransaction();
        try
        {
            await connection.ExecuteAsync(
                new CommandDefinition(
                    """
                    UPDATE tblTenantMembership
                    SET DefaultBranchId = @BranchId,
                        MembershipStatus = @MembershipStatus,
                        Active = CASE WHEN @MembershipStatus = 'Removed' THEN 0 ELSE 1 END
                    WHERE TenantMembershipId = @MembershipId
                      AND TenantId = @TenantId;

                    DELETE membershipRole
                    FROM tblTenantMembershipRole membershipRole
                    INNER JOIN tblTenantRole role
                        ON role.TenantRoleId = membershipRole.TenantRoleId
                    WHERE membershipRole.TenantMembershipId = @MembershipId
                      AND role.TenantId = @TenantId;
                    """,
                    new
                    {
                        request.MembershipId,
                        context.TenantId,
                        BranchId = request.BranchId,
                        request.MembershipStatus
                    },
                    transaction,
                    cancellationToken: cancellationToken));

            if (request.MembershipStatus != "Removed")
            {
                await InsertRolesAsync(
                    connection,
                    transaction,
                    context.TenantId,
                    request.MembershipId,
                    context.UserId,
                    roleCodes,
                    cancellationToken);
            }

            transaction.Commit();
            return new TenantUserOperationResult(true, "Tenant membership updated successfully.");
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    private async Task<TenantAccessContext> RequireManagerAsync(CancellationToken cancellationToken)
    {
        var context = await _tenantAuthorizationService.GetAccessContextAsync(cancellationToken);
        if (!context.RoleCodes.Contains(TenantRoleCodes.Owner)
            && !context.RoleCodes.Contains(TenantRoleCodes.Administrator))
        {
            throw new UnauthorizedAccessException(
                "Only Tenant Owners and Tenant Administrators can manage tenant users and roles.");
        }

        return context;
    }

    private static HashSet<string> NormalizeRoles(IEnumerable<string> roleCodes) =>
        roleCodes
            .Where(code => !string.IsNullOrWhiteSpace(code))
            .Select(code => code.Trim().ToUpperInvariant())
            .Where(AssignableRoleCodes.Contains)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

    private static async Task InsertRolesAsync(
        IDbConnection connection,
        IDbTransaction transaction,
        long tenantId,
        long membershipId,
        long assignedBy,
        IReadOnlyCollection<string> roleCodes,
        CancellationToken cancellationToken)
    {
        var inserted = await connection.ExecuteAsync(
            new CommandDefinition(
                """
                INSERT INTO tblTenantMembershipRole
                    (TenantMembershipId, TenantRoleId, AssignedDate, AssignedBy)
                SELECT
                    @MembershipId,
                    role.TenantRoleId,
                    SYSUTCDATETIME(),
                    @AssignedBy
                FROM tblTenantRole role
                WHERE role.TenantId = @TenantId
                  AND role.Active = 1
                  AND role.RoleCode IN @RoleCodes;
                """,
                new
                {
                    MembershipId = membershipId,
                    AssignedBy = assignedBy,
                    TenantId = tenantId,
                    RoleCodes = roleCodes
                },
                transaction,
                cancellationToken: cancellationToken));
        if (inserted != roleCodes.Count)
            throw new InvalidOperationException("One or more selected tenant roles are unavailable.");
    }

    private static async Task<bool> HasSeatAvailableAsync(
        IDbConnection connection,
        long tenantId,
        CancellationToken cancellationToken)
    {
        var counts = await connection.QuerySingleAsync<SeatCounts>(
            new CommandDefinition(
                """
                SELECT
                    subscription.SeatQuantity,
                    (
                        SELECT COUNT(1)
                        FROM tblTenantMembership membership
                        WHERE membership.TenantId = @TenantId
                          AND membership.Active = 1
                          AND membership.MembershipStatus = 'Active'
                    ) AS ActiveMembers
                FROM tblTenantSubscription subscription
                WHERE subscription.TenantId = @TenantId
                  AND subscription.Active = 1;
                """,
                new { TenantId = tenantId },
                cancellationToken: cancellationToken));
        return counts.ActiveMembers < counts.SeatQuantity;
    }

    private static async Task<bool> BranchIsValidAsync(
        IDbConnection connection,
        long tenantId,
        long? branchId,
        CancellationToken cancellationToken)
    {
        if (branchId is null or <= 0)
            return true;
        return await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(
                """
                SELECT COUNT(1)
                FROM tblBranch
                WHERE BranchId = @BranchId
                  AND TenantId = @TenantId
                  AND Active = 1;
                """,
                new { BranchId = branchId, TenantId = tenantId },
                cancellationToken: cancellationToken)) == 1;
    }

    private static Task<ExistingMembership?> GetExistingMembershipAsync(
        IDbConnection connection,
        long tenantId,
        long userId,
        CancellationToken cancellationToken) =>
        connection.QuerySingleOrDefaultAsync<ExistingMembership>(
            new CommandDefinition(
                """
                SELECT
                    TenantMembershipId AS MembershipId,
                    MembershipStatus
                FROM tblTenantMembership
                WHERE TenantId = @TenantId
                  AND UserId = @UserId;
                """,
                new { TenantId = tenantId, UserId = userId },
                cancellationToken: cancellationToken));

    private static async Task RemoveMembershipAsync(
        IDbConnection connection,
        long membershipId,
        CancellationToken cancellationToken)
    {
        await connection.ExecuteAsync(
            new CommandDefinition(
                """
                DELETE FROM tblTenantMembershipRole
                WHERE TenantMembershipId = @MembershipId;
                DELETE FROM tblTenantMembership
                WHERE TenantMembershipId = @MembershipId;
                """,
                new { MembershipId = membershipId },
                cancellationToken: cancellationToken));
    }

    private async Task<EmailDeliveryResult> SendWelcomeEmailAsync(
        ApplicationUser user,
        string? temporaryPassword,
        bool createdAccount,
        CancellationToken cancellationToken)
    {
        var displayName = WebUtility.HtmlEncode(
            $"{user.FirstName} {user.LastName}".Trim());
        var tenantName = WebUtility.HtmlEncode(_sessionService.TenantName);
        var body = createdAccount
            ? $"""
               <p>Hello {displayName},</p>
               <p>An account has been created for you in <strong>{tenantName}</strong> on ViabilityIQ.</p>
               <p><strong>Username:</strong> {WebUtility.HtmlEncode(user.Email)}<br />
               <strong>Temporary password:</strong> {WebUtility.HtmlEncode(temporaryPassword)}</p>
               <p>Sign in and change this temporary password immediately. Access to the application remains restricted until the password is changed.</p>
               """
            : $"""
               <p>Hello {displayName},</p>
               <p>Your existing ViabilityIQ account has been added to <strong>{tenantName}</strong>.</p>
               <p>Use your existing username and password to sign in.</p>
               """;

        return await _emailService.SendReportAsync(
            new EmailReportRequest
            {
                RecipientAddress = user.Email!,
                SubjectTitle = createdAccount
                    ? "Your ViabilityIQ account"
                    : "ViabilityIQ tenant access granted",
                MessageBodyText = body
            },
            cancellationToken);
    }

    private static string GenerateTemporaryPassword()
    {
        const string lower = "abcdefghijkmnopqrstuvwxyz";
        const string upper = "ABCDEFGHJKLMNPQRSTUVWXYZ";
        const string digits = "23456789";
        const string symbols = "!@$%*?";
        const string all = lower + upper + digits + symbols;
        var characters = new List<char>
        {
            lower[RandomNumberGenerator.GetInt32(lower.Length)],
            upper[RandomNumberGenerator.GetInt32(upper.Length)],
            digits[RandomNumberGenerator.GetInt32(digits.Length)],
            symbols[RandomNumberGenerator.GetInt32(symbols.Length)]
        };
        for (var index = characters.Count; index < 14; index++)
            characters.Add(all[RandomNumberGenerator.GetInt32(all.Length)]);
        return new string(characters.OrderBy(_ => RandomNumberGenerator.GetInt32(int.MaxValue)).ToArray());
    }

    private static async Task OpenAsync(IDbConnection connection, CancellationToken cancellationToken)
    {
        if (connection.State == ConnectionState.Open)
            return;
        if (connection is System.Data.Common.DbConnection dbConnection)
            await dbConnection.OpenAsync(cancellationToken);
        else
            connection.Open();
    }

    private static TenantUserOperationResult Failed(string message) => new(false, message);

    private sealed class TenantHeader
    {
        public string TenantName { get; init; } = string.Empty;
        public int SeatQuantity { get; init; }
        public int ActiveMemberCount { get; init; }
    }

    private sealed class RoleAssignment
    {
        public long MembershipId { get; init; }
        public string RoleCode { get; init; } = string.Empty;
        public string RoleName { get; init; } = string.Empty;
    }

    private sealed class SeatCounts
    {
        public int SeatQuantity { get; init; }
        public int ActiveMembers { get; init; }
    }

    private sealed class MemberTarget
    {
        public long TenantMembershipId { get; init; }
        public long UserId { get; init; }
        public bool IsOwner { get; init; }
        public string MembershipStatus { get; init; } = string.Empty;
    }

    private sealed class ExistingMembership
    {
        public long MembershipId { get; init; }
        public string MembershipStatus { get; init; } = string.Empty;
    }
}
