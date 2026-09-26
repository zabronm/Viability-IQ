using System.ComponentModel;

namespace ViabilityIQ.Shared.Reporting;

public enum OperationalReportType
{
    AssessmentRegister = 1,
    WorkflowReadiness = 2,
    AdvisorPerformance = 3,
    BranchPerformance = 4
}

public sealed class AssessmentOperationalReportFilter
{
    public string? SearchText { get; set; }
    public long? BranchId { get; set; }
    public long? StatusId { get; set; }
    public long? AdvisorId { get; set; }
    public DateTime? StartDateFrom { get; set; }
    public DateTime? StartDateTo { get; set; }
    public bool ActiveOnly { get; set; } = true;
}

public sealed record OperationalReportOption(long Id, string Name);

public sealed class OperationalReportFilterOptions
{
    public IReadOnlyList<OperationalReportOption> Branches { get; init; } =
        Array.Empty<OperationalReportOption>();
    public IReadOnlyList<OperationalReportOption> Advisors { get; init; } =
        Array.Empty<OperationalReportOption>();
}

public sealed class AssessmentOperationalReportRow
{
    [DisplayName("Case Number")]
    public string CaseNumber { get; init; } = string.Empty;

    [DisplayName("Business")]
    public string BusinessName { get; init; } = string.Empty;

    [DisplayName("Client")]
    public string ClientName { get; init; } = string.Empty;

    [DisplayName("Branch")]
    public string BranchName { get; init; } = "Unassigned";

    [DisplayName("Advisor")]
    public string AdvisorName { get; init; } = "Unassigned";

    [DisplayName("Status")]
    public string StatusName { get; init; } = string.Empty;

    [DisplayName("Readiness %")]
    public long ProgressPercentage { get; init; }

    [DisplayName("Start Date")]
    public DateTime? AssessmentStartDate { get; init; }

    [DisplayName("Expected Finish")]
    public DateTime? AssessmentFinishDate { get; init; }

    [DisplayName("Created")]
    public DateTime CreatedDate { get; init; }

    [DisplayName("Last Updated")]
    public DateTime ModifiedDate { get; init; }

    [DisplayName("Days Open")]
    public int DaysOpen { get; init; }

    [DisplayName("Active")]
    public bool Active { get; init; }

    public long AssessmentId { get; init; }
    public long StatusId { get; init; }
    public long AdvisorId { get; init; }
    public long? BranchId { get; init; }
}

public sealed class AssessmentRegisterExportRow
{
    [DisplayName("Case Number")]
    public string CaseNumber { get; init; } = string.Empty;
    [DisplayName("Business")]
    public string BusinessName { get; init; } = string.Empty;
    [DisplayName("Client")]
    public string ClientName { get; init; } = string.Empty;
    [DisplayName("Branch")]
    public string BranchName { get; init; } = string.Empty;
    [DisplayName("Advisor")]
    public string AdvisorName { get; init; } = string.Empty;
    [DisplayName("Status")]
    public string StatusName { get; init; } = string.Empty;
    [DisplayName("Readiness %")]
    public long ReadinessPercentage { get; init; }
    [DisplayName("Start Date")]
    public DateTime? StartDate { get; init; }
    [DisplayName("Expected Finish")]
    public DateTime? ExpectedFinishDate { get; init; }
    [DisplayName("Days Open")]
    public int DaysOpen { get; init; }
    [DisplayName("Active")]
    public bool Active { get; init; }
}

public sealed class WorkflowReadinessReportRow
{
    [DisplayName("Case Number")]
    public string CaseNumber { get; init; } = string.Empty;
    [DisplayName("Business")]
    public string BusinessName { get; init; } = string.Empty;
    [DisplayName("Status")]
    public string StatusName { get; init; } = string.Empty;
    [DisplayName("Readiness %")]
    public long ReadinessPercentage { get; init; }
    [DisplayName("Advisor")]
    public string AdvisorName { get; init; } = string.Empty;
    [DisplayName("Branch")]
    public string BranchName { get; init; } = string.Empty;
    [DisplayName("Start Date")]
    public DateTime? StartDate { get; init; }
    [DisplayName("Last Updated")]
    public DateTime LastUpdated { get; init; }
    [DisplayName("Days Open")]
    public int DaysOpen { get; init; }
}

public sealed class AdvisorPerformanceReportRow
{
    [DisplayName("Advisor")]
    public string AdvisorName { get; init; } = string.Empty;
    [DisplayName("Branch")]
    public string BranchName { get; init; } = string.Empty;
    [DisplayName("Assessments")]
    public int TotalAssessments { get; init; }
    [DisplayName("Draft")]
    public int DraftCount { get; init; }
    [DisplayName("In Progress")]
    public int InProgressCount { get; init; }
    [DisplayName("Ready for Review")]
    public int ReadyForReviewCount { get; init; }
    [DisplayName("Completed")]
    public int CompletedCount { get; init; }
    [DisplayName("Average Readiness %")]
    public decimal AverageReadiness { get; init; }
    [DisplayName("Completion Rate %")]
    public decimal CompletionRate { get; init; }
}

public sealed class BranchPerformanceReportRow
{
    [DisplayName("Branch")]
    public string BranchName { get; init; } = string.Empty;
    [DisplayName("Advisors")]
    public int AdvisorCount { get; init; }
    [DisplayName("Assessments")]
    public int TotalAssessments { get; init; }
    [DisplayName("Draft")]
    public int DraftCount { get; init; }
    [DisplayName("In Progress")]
    public int InProgressCount { get; init; }
    [DisplayName("Ready for Review")]
    public int ReadyForReviewCount { get; init; }
    [DisplayName("Completed")]
    public int CompletedCount { get; init; }
    [DisplayName("Average Readiness %")]
    public decimal AverageReadiness { get; init; }
    [DisplayName("Completion Rate %")]
    public decimal CompletionRate { get; init; }
}
