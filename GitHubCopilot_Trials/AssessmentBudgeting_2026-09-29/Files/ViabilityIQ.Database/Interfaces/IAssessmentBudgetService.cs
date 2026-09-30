using ViabilityIQ.Shared.SharedModels;

namespace ViabilityIQ.Application.Interfaces;

public interface IAssessmentBudgetService
{
    Task<AssessmentBudgetWorkspace?> GetAsync(
        long assessmentId,
        CancellationToken cancellationToken = default);

    Task<AssessmentBudgetWorkspace> CreateFromProjectionAsync(
        CreateAssessmentBudgetRequest request,
        CancellationToken cancellationToken = default);

    Task<AssessmentBudgetWorkspace> SaveAsync(
        SaveAssessmentBudgetRequest request,
        CancellationToken cancellationToken = default);

    Task<AssessmentBudgetWorkspace> RefreshProjectionAsync(
        long assessmentId,
        long budgetId,
        byte[] rowVersion,
        CancellationToken cancellationToken = default);

    Task<AssessmentBudgetWorkspace> SubmitAsync(
        long assessmentId,
        long budgetId,
        byte[] rowVersion,
        CancellationToken cancellationToken = default);

    Task<AssessmentBudgetWorkspace> ApproveAsync(
        long assessmentId,
        long budgetId,
        byte[] rowVersion,
        CancellationToken cancellationToken = default);

    Task<AssessmentBudgetWorkspace> ReopenAsync(
        long assessmentId,
        long budgetId,
        byte[] rowVersion,
        CancellationToken cancellationToken = default);

    IReadOnlyList<AssessmentBudgetAlert> BuildAlerts(
        AssessmentBudgetWorkspace workspace);
}
