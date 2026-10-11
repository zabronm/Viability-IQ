namespace ViabilityIQ.Application.Dtos;

public sealed class TenantProvisioningRequest
{
    public long UserId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string PlanCode { get; set; } = "STANDARD";
    public string OrganisationName { get; set; } = string.Empty;
    public int RequestedSeats { get; set; } = 1;
    public string CompanyCustomerCode { get; set; } = string.Empty;
    public string CompanyRegistrationNumber { get; set; } = string.Empty;
    public string CompanyContactPerson { get; set; } = string.Empty;
    public string CompanyEmail { get; set; } = string.Empty;
    public string CompanyTelephone { get; set; } = string.Empty;
    public string CompanyMobile { get; set; } = string.Empty;
    public string CompanyWebsite { get; set; } = string.Empty;
    public string CompanyStreetAddress { get; set; } = string.Empty;
    public string CompanySuburb { get; set; } = string.Empty;
    public string CompanyCityTown { get; set; } = string.Empty;
    public string CompanyPostalCode { get; set; } = string.Empty;
    public string CompanyCountry { get; set; } = string.Empty;
    public string InitialRegionName { get; set; } = string.Empty;
    public string InitialSiteName { get; set; } = string.Empty;
}

public sealed class TenantProvisioningResult
{
    public bool Success { get; set; }
    public long TenantId { get; set; }
    public long CompanyId { get; set; }
    public long RegionId { get; set; }
    public long SiteId { get; set; }
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
    public long BranchId { get; set; }
    public long ProvinceId { get; set; }
    public bool IsOwner { get; set; }
}
