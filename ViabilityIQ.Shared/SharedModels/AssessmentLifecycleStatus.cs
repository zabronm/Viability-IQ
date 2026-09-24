namespace ViabilityIQ.Shared.SharedModels;

public static class AssessmentLifecycleStatus
{
    public const long Draft = 1;
    public const long InProgress = 2;
    public const long ReadyForReview = 3;
    public const long Completed = 4;
    public const long Archived = 5;

    public static string GetName(long statusId) => statusId switch
    {
        Draft => "Draft",
        InProgress => "In Progress",
        ReadyForReview => "Ready for Review",
        Completed => "Completed",
        Archived => "Archived",
        _ => "Unknown"
    };
}
