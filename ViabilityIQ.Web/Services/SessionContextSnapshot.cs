namespace ViabilityIQ.Web.Services;

public sealed record SessionContextSnapshot(
    long UserId,
    long TenantMembershipId,
    long? AssessmentId);
