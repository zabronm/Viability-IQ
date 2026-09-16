using Microsoft.AspNetCore.Components;
using ViabilityIQ.Shared.FinancialModels;

namespace ViabilityIQ.Web.Components.Pages_Assessments.ProjectionComponents;

public partial class BusinessHealthAlertsComponent : ComponentBase
{
    [Parameter, EditorRequired] public CashflowProjectionResult Projection { get; set; } = default!;
}
