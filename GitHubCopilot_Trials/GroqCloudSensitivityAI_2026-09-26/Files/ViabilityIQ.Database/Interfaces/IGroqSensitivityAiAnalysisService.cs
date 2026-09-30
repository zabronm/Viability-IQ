using ViabilityIQ.Shared.FinancialModels;

namespace ViabilityIQ.Application.Interfaces;

public interface IGroqSensitivityAiAnalysisService
{
    SensitivityAiConfiguration GetConfiguration();

    Task<SensitivityAiAnalysisResult> AnalyseAsync(
        SensitivityAiAnalysisRequest request,
        bool consentGiven,
        CancellationToken cancellationToken = default);
}

