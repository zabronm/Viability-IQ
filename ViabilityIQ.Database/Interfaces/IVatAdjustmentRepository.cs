using ViabilityIQ.Shared.FinancialModels;

namespace ViabilityIQ.Application.Interfaces;

public interface IVatAdjustmentRepository
{
    Task<IReadOnlyList<VatAdjustmentMonth>> GetActiveAsync(long assessmentId);
    Task SaveAsync(SaveVatAdjustmentsRequest request);
}
