using ViabilityIQ.Shared.DataModels.SecurityDataModels;

namespace ViabilityIQ.Application.Interfaces;

public interface ITenantAuthorizationService
{
    Task<TenantAccessContext> GetAccessContextAsync(
        CancellationToken cancellationToken = default);

    Task EnsureCanReadOperationalDataAsync(
        CancellationToken cancellationToken = default);

    Task EnsureCanCreateOperationalDataAsync(
        CancellationToken cancellationToken = default);

    Task EnsureCanAccessAssessmentAsync(
        long assessmentId,
        TenantRecordAccess access,
        CancellationToken cancellationToken = default);
}
