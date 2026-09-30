using ViabilityIQ.Shared.FinancialModels;

namespace ViabilityIQ.Application.Interfaces;

public interface ICashflowProjectionService
{
    Task<CashflowProjectionResult> CalculateAsync(long assessmentId);
}
