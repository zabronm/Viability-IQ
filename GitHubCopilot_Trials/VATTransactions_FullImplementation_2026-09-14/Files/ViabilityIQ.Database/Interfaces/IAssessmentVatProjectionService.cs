using ViabilityIQ.Shared.FinancialModels;

namespace ViabilityIQ.Application.Interfaces;

public interface IAssessmentVatProjectionService
{
    Task<IReadOnlyList<MonthlyVatProjection>> CalculateAsync(long assessmentId);
}
