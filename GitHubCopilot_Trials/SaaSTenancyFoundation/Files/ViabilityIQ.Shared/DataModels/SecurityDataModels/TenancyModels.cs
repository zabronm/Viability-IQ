namespace ViabilityIQ.Shared.DataModels.SecurityDataModels;

public static class TenantTypes
{
    public const string Personal = "Personal";
    public const string Organisation = "Organisation";
}

public static class SubscriptionPlanCodes
{
    public const string Standard = "STANDARD";
    public const string Open = "OPEN";
}

public static class TenantMembershipStatuses
{
    public const string Active = "Active";
    public const string Invited = "Invited";
    public const string Suspended = "Suspended";
    public const string Removed = "Removed";
}

public static class TenantSubscriptionStatuses
{
    public const string Trial = "Trial";
    public const string Active = "Active";
    public const string PastDue = "PastDue";
    public const string Suspended = "Suspended";
    public const string Cancelled = "Cancelled";
}

public static class TenantRoleCodes
{
    public const string Owner = "TENANT_OWNER";
    public const string Administrator = "TENANT_ADMIN";
    public const string BranchManager = "BRANCH_MANAGER";
    public const string Viewer = "VIEWER";
    public const string BillingManager = "BILLING_MANAGER";
}

public sealed class Tenant
{
    public long TenantId { get; set; }
    public string TenantType { get; set; } = TenantTypes.Personal;
    public string TenantName { get; set; } = string.Empty;
    public string TenantSlug { get; set; } = string.Empty;
    public long OwnerUserId { get; set; }
    public long? ProvinceId { get; set; }
    public string Status { get; set; } = TenantSubscriptionStatuses.Active;
    public bool Active { get; set; } = true;
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public long CreatedBy { get; set; }
}

public sealed class TenantMembership
{
    public long TenantMembershipId { get; set; }
    public long TenantId { get; set; }
    public long UserId { get; set; }
    public string MembershipStatus { get; set; } = TenantMembershipStatuses.Active;
    public bool IsOwner { get; set; }
    public long? DefaultBranchId { get; set; }
    public DateTime JoinedDate { get; set; } = DateTime.UtcNow;
    public bool Active { get; set; } = true;
}

public sealed class SubscriptionPlan
{
    public long SubscriptionPlanId { get; set; }
    public string PlanCode { get; set; } = string.Empty;
    public string PlanName { get; set; } = string.Empty;
    public string TenantType { get; set; } = TenantTypes.Personal;
    public int IncludedSeats { get; set; }
    public bool AllowsAdditionalSeats { get; set; }
    public int TrialDays { get; set; }
    public bool Active { get; set; } = true;
}

public sealed class TenantSubscription
{
    public long TenantSubscriptionId { get; set; }
    public long TenantId { get; set; }
    public long SubscriptionPlanId { get; set; }
    public string Status { get; set; } = TenantSubscriptionStatuses.Active;
    public int SeatQuantity { get; set; }
    public DateTime CurrentPeriodStart { get; set; }
    public DateTime? CurrentPeriodEnd { get; set; }
    public DateTime? TrialEndsAt { get; set; }
    public bool CancelAtPeriodEnd { get; set; }
    public bool Active { get; set; } = true;
}

public sealed class TenantInvitation
{
    public long TenantInvitationId { get; set; }
    public long TenantId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string RoleCode { get; set; } = TenantRoleCodes.Viewer;
    public long? BranchId { get; set; }
    public string TokenHash { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public DateTime? AcceptedAt { get; set; }
    public long InvitedBy { get; set; }
    public string Status { get; set; } = TenantMembershipStatuses.Invited;
    public bool Active { get; set; } = true;
}
