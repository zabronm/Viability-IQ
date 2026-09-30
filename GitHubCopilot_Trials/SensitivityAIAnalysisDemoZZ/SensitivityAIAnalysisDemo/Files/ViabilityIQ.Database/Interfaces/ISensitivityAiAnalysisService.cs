using ViabilityIQ.Shared.FinancialModels;

namespace ViabilityIQ.Application.Interfaces;

public interface ISensitivityAiAnalysisService
{
    SensitivityAiConfiguration GetConfiguration();

    Task<SensitivityAiAnalysisResult> AnalyseAsync(
        SensitivityAiAnalysisRequest request,
        bool consentGiven,
        CancellationToken cancellationToken = default);
}
