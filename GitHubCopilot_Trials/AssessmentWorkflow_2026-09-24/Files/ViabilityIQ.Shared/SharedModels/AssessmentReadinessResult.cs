namespace ViabilityIQ.Shared.SharedModels;

public sealed class AssessmentReadinessResult
{
    public long AssessmentId { get; init; }
    public int Score { get; init; }
    public bool CanComplete { get; init; }
    public IReadOnlyList<AssessmentReadinessCheck> Checks { get; init; } = [];
    public int CompletedChecks => Checks.Count(check => check.Passed);
    public int OutstandingChecks => Checks.Count - CompletedChecks;
}

public sealed record AssessmentReadinessCheck(
    string Title,
    bool Passed,
    string Message,
    string Action,
    bool Required);
