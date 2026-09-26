namespace ViabilityIQ.Application.Dtos;

public sealed class TenantProvisioningRequest
{
    public long UserId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string PlanCode { get; set; } = "STANDARD";
    public string OrganisationName { get; set; } = string.Empty;
    public int RequestedSeats { get; set; } = 1;
    public long? ProvinceId { get; set; }
}

public sealed class TenantProvisioningResult
{
    public bool Success { get; set; }
    public long TenantId { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
}

public sealed class TenantContextDto
{
    public long TenantId { get; set; }
    public string TenantName { get; set; } = string.Empty;
    public string TenantType { get; set; } = string.Empty;
    public string PlanCode { get; set; } = string.Empty;
    public string SubscriptionStatus { get; set; } = string.Empty;
    public int SeatQuantity { get; set; }
    public long MembershipId { get; set; }
    public bool IsOwner { get; set; }
}
