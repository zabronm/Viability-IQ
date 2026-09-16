using Microsoft.AspNetCore.Components;
using ViabilityIQ.Shared.FinancialModels;

namespace ViabilityIQ.Web.Components.Pages_Assessments.ProjectionComponents;

public partial class CashflowSummaryTableComponent : ComponentBase
{
    [Parameter, EditorRequired] public CashflowProjectionResult Projection { get; set; } = default!;
}
