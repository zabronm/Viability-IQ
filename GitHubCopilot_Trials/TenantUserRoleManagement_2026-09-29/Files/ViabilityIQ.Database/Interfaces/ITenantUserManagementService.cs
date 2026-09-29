using ViabilityIQ.Application.Dtos;

namespace ViabilityIQ.Application.Interfaces;

public interface ITenantUserManagementService
{
    Task<TenantUserManagementSnapshot> GetSnapshotAsync(
        CancellationToken cancellationToken = default);

    Task<TenantUserOperationResult> CreateMemberAsync(
        CreateTenantUserRequest request,
        CancellationToken cancellationToken = default);

    Task<TenantUserOperationResult> UpdateMemberAsync(
        UpdateTenantMemberRequest request,
        CancellationToken cancellationToken = default);
}

