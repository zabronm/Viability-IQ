using ViabilityIQ.Shared.SharedModels;

namespace ViabilityIQ.Application.Interfaces;

public interface IAssessmentReadinessService
{
    Task<AssessmentReadinessResult> EvaluateAsync(long assessmentId);
}
