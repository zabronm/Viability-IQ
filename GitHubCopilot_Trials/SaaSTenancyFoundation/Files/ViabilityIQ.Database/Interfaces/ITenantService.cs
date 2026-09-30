using ViabilityIQ.Application.Dtos;

namespace ViabilityIQ.Application.Interfaces;

public interface ITenantService
{
    Task<TenantProvisioningResult> ProvisionTenantAsync(
        TenantProvisioningRequest request,
        CancellationToken cancellationToken = default);

    Task<TenantContextDto?> GetDefaultTenantAsync(
        long userId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TenantContextDto>> GetUserTenantsAsync(
        long userId,
        CancellationToken cancellationToken = default);
}
