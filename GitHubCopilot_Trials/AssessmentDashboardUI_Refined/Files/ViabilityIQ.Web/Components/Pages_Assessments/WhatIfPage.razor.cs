using Microsoft.AspNetCore.Components;
using ViabilityIQ.Application.Interfaces;
using ViabilityIQ.Web.Services;

namespace ViabilityIQ.Web.Components.Pages_Assessments;

public partial class WhatIfPage
{
    [Inject] private ISessionService SessionService { get; set; } = default!;
    [Inject] private ToastService Toast { get; set; } = default!;

    [Parameter] public long AssessmentId { get; set; }

    private long ActiveAssessmentId { get; set; }
    private string? ErrorMessage { get; set; }

    protected override void OnParametersSet()
    {
        ActiveAssessmentId = AssessmentId > 0
            ? AssessmentId
            : SessionService.AssessmentId.GetValueOrDefault();

        ErrorMessage = ActiveAssessmentId > 0
            ? null
            : "No active assessment was found.";

        if (ActiveAssessmentId <= 0)
            Toast.ShowError("Case number is unknown. Please restart the application.");
    }
}
