using Microsoft.AspNetCore.Components;

namespace ViabilityIQ.Web.Components.Pages_Assessments.CommonComponents;

public partial class AssessmentKpiCardsComponent
{
    private string HeaderId { get; } = $"assessment-kpi-title-{Guid.NewGuid():N}";

    [Parameter, EditorRequired]    public IReadOnlyList<AssessmentKpiCardItem> Items { get; set; } = [];
    [Parameter]    public string AriaLabel { get; set; } = "Key performance indicators";
    [Parameter]    public string HeaderTitle { get; set; } = "Assessment Performance Metrics";
    [Parameter]    public string HeaderSubtitle { get; set; } = "Quick KPI metrics";
    [Parameter]    public string HeaderIcon { get; set; } = "bi-speedometer2";
}

public sealed record AssessmentKpiCardItem(
    string Title,
    string Value,
    string Caption,
    string Icon,
    string AccentClass);
