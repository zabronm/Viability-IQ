namespace ViabilityIQ.Shared.SharedModels;

public static class AssessmentBudgetStatuses
{
    public const string Draft = "Draft";
    public const string Submitted = "Submitted";
    public const string Approved = "Approved";
}

public static class AssessmentBudgetSections
{
    public const string ProfitAndLoss = "ProfitAndLoss";
    public const string CashFlow = "CashFlow";
}

public sealed class AssessmentBudgetWorkspace
{
    public long BudgetId { get; init; }
    public long AssessmentId { get; init; }
    public string BudgetName { get; set; } = string.Empty;
    public string Status { get; init; } = AssessmentBudgetStatuses.Draft;
    public string? Remarks { get; set; }
    public DateTime? AssessmentStartDate { get; init; }
    public DateTime ProjectionCalculatedAtUtc { get; init; }
    public DateTime ProjectionImportedAtUtc { get; init; }
    public DateTime ModifiedDate { get; init; }
    public decimal OpeningBank { get; init; }
    public bool CanEdit { get; init; }
    public bool CanApprove { get; init; }
    public byte[] RowVersion { get; init; } = [];
    public List<AssessmentBudgetLine> Lines { get; init; } = [];

    public decimal ProjectedProfitBeforeTax =>
        FindLine("PBT")?.ProjectionTotal ?? 0m;

    public decimal BudgetProfitBeforeTax =>
        FindLine("PBT")?.BudgetTotal ?? 0m;

    public decimal ProjectedNetCashflow =>
        FindLine("NET_CASH")?.ProjectionTotal ?? 0m;

    public decimal BudgetNetCashflow =>
        FindLine("NET_CASH")?.BudgetTotal ?? 0m;

    public decimal ProjectedClosingBank =>
        FindLine("CLOSING_BANK")?.ProjectedMonths.LastOrDefault() ?? 0m;

    public decimal BudgetClosingBank =>
        FindLine("CLOSING_BANK")?.BudgetMonths.LastOrDefault() ?? 0m;

    public AssessmentBudgetLine? FindLine(string lineCode) =>
        Lines.FirstOrDefault(line =>
            string.Equals(line.LineCode, lineCode, StringComparison.OrdinalIgnoreCase));
}

public sealed class AssessmentBudgetLine
{
    public long BudgetLineId { get; init; }
    public string SectionCode { get; init; } = string.Empty;
    public string SectionName { get; init; } = string.Empty;
    public string LineCode { get; init; } = string.Empty;
    public string LineName { get; init; } = string.Empty;
    public int DisplayOrder { get; init; }
    public bool IsCalculated { get; init; }
    public decimal[] ProjectedMonths { get; init; } = new decimal[12];
    public decimal[] BudgetMonths { get; init; } = new decimal[12];

    public decimal ProjectionTotal => ProjectedMonths.Sum();
    public decimal BudgetTotal => BudgetMonths.Sum();
    public decimal VarianceTotal => BudgetTotal - ProjectionTotal;
    public decimal VariancePercentage =>
        ProjectionTotal == 0m ? 0m : VarianceTotal / Math.Abs(ProjectionTotal) * 100m;
}

public sealed class CreateAssessmentBudgetRequest
{
    public long AssessmentId { get; init; }
    public string BudgetName { get; init; } = string.Empty;
    public string? Remarks { get; init; }
}

public sealed class SaveAssessmentBudgetRequest
{
    public long AssessmentId { get; init; }
    public long BudgetId { get; init; }
    public string BudgetName { get; init; } = string.Empty;
    public string? Remarks { get; init; }
    public byte[] RowVersion { get; init; } = [];
    public IReadOnlyList<SaveAssessmentBudgetLineRequest> Lines { get; init; } = [];
}

public sealed class SaveAssessmentBudgetLineRequest
{
    public long BudgetLineId { get; init; }
    public decimal[] BudgetMonths { get; init; } = new decimal[12];
}

public sealed class AssessmentBudgetAlert
{
    public string Severity { get; init; } = "Information";
    public string Title { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
}

