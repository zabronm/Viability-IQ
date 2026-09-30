using System.ComponentModel.DataAnnotations;

namespace ViabilityIQ.Application.Dtos;

public sealed class TenantUserManagementSnapshot
{
    public bool CanManageUsers { get; init; }
    public string TenantName { get; init; } = string.Empty;
    public int SeatQuantity { get; init; }
    public int ActiveMemberCount { get; init; }
    public IReadOnlyList<TenantMemberRow> Members { get; init; } = [];
    public IReadOnlyList<TenantRoleOption> Roles { get; init; } = [];
    public IReadOnlyList<TenantBranchOption> Branches { get; init; } = [];
}

public sealed class TenantMemberRow
{
    public long MembershipId { get; init; }
    public long UserId { get; init; }
    public string FullName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string MembershipStatus { get; init; } = string.Empty;
    public bool IsOwner { get; init; }
    public long? BranchId { get; init; }
    public string BranchName { get; init; } = "Unassigned";
    public DateTime? LastLoginAt { get; init; }
    public bool MustChangePassword { get; init; }
    public IReadOnlyList<string> RoleCodes { get; set; } = [];
    public IReadOnlyList<string> RoleNames { get; set; } = [];
}

public sealed class TenantRoleOption
{
    public long RoleId { get; init; }
    public string RoleCode { get; init; } = string.Empty;
    public string RoleName { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public bool IsProtected { get; init; }
}

public sealed class TenantBranchOption
{
    public long BranchId { get; init; }
    public string BranchName { get; init; } = string.Empty;
}

public sealed class CreateTenantUserRequest
{
    [Required, StringLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string LastName { get; set; } = string.Empty;

    [Required, EmailAddress, StringLength(320)]
    public string Email { get; set; } = string.Empty;

    public long? BranchId { get; set; }
    public HashSet<string> RoleCodes { get; set; } = [];
}

public sealed class UpdateTenantMemberRequest
{
    public long MembershipId { get; set; }
    public long? BranchId { get; set; }
    public string MembershipStatus { get; set; } = "Active";
    public HashSet<string> RoleCodes { get; set; } = [];
}

public sealed record TenantUserOperationResult(
    bool Succeeded,
    string Message,
    bool ExistingAccount = false);

