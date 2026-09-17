using ViabilityIQ.Shared.FinancialModels;

namespace ViabilityIQ.Application.Interfaces;

public interface ISensitivityAnalysisService
{
    Task<ScenarioAnalysisResult> CalculateScenarioAsync(
        long assessmentId,
        SensitivityScenario scenario,
        CancellationToken cancellationToken = default);

    Task<SensitivitySweepResult> RunSweepAsync(
        long assessmentId,
        SensitivitySweepRequest request,
        CancellationToken cancellationToken = default);

    Task<SensitivityRiskResult> AnalyseRisksAsync(
        long assessmentId,
        decimal minimumCashReserve,
        CancellationToken cancellationToken = default);
}
