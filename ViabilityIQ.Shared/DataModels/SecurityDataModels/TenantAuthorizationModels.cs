namespace ViabilityIQ.Shared.DataModels.SecurityDataModels;

public static class TenantRecordTypes
{
    public const string Assessment = "Assessment";
    public const string Business = "Business";
    public const string Client = "Client";
    public const string Company = "Company";
    public const string Branch = "Branch";
}

public enum TenantRecordAccess
{
    Read,
    Write,
    Delete
}

public sealed class TenantAccessContext
{
    public long TenantId { get; init; }
    public long UserId { get; init; }
    public long MembershipId { get; init; }
    public IReadOnlySet<string> RoleCodes { get; init; } = new HashSet<string>();
    public bool CanReadAllOperationalRecords { get; init; }
    public bool CanWriteAllOperationalRecords { get; init; }
    public bool CanReadScopedOperationalRecords { get; init; }
    public bool CanWriteScopedOperationalRecords { get; init; }
    public bool CanDeleteScopedOperationalRecords { get; init; }
}
