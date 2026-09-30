using Microsoft.AspNetCore.Components;

namespace ViabilityIQ.Web.Components.Pages_Assessments.CommonComponents;

public partial class AssessmentKpiCardsComponent
{
    [Parameter, EditorRequired]
    public IReadOnlyList<AssessmentKpiCardItem> Items { get; set; } = [];

    [Parameter]
    public string AriaLabel { get; set; } = "Key performance indicators";
}

public sealed record AssessmentKpiCardItem(
    string Title,
    string Value,
    string Caption,
    string Icon,
    string AccentClass);
