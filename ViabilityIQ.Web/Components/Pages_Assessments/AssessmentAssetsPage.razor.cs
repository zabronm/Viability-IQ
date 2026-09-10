using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using ViabilityIQ.Application.Interfaces;

namespace ViabilityIQ.Web.Components.Pages_Assessments;

public partial class AssessmentAssetsPage : ComponentBase, IDisposable
{
    [Inject] private ISessionService? SessionService { get; set; }
    [Inject] private ILogger<AssessmentAssetsPage> Logger { get; set; } = default!;

    [CascadingParameter(Name = "CurrentAssessmentId")]
    public long? CascadedAssessmentId { get; set; }

    [Parameter] public long RouteAssessmentId { get; set; }

    private long AssessmentId { get; set; }
    private AssetPageTab ActiveTab { get; set; } = AssetPageTab.Summary;

    protected override void OnInitialized()
    {
        if (SessionService != null)
        {
            SessionService.OnSessionChanged += OnSessionChanged;
        }

        ResolveAssessmentContext();
    }

    protected override void OnParametersSet()
    {
        ResolveAssessmentContext();
    }

    private void ResolveAssessmentContext()
    {
        AssessmentId = RouteAssessmentId > 0
            ? RouteAssessmentId
            : CascadedAssessmentId is > 0
                ? CascadedAssessmentId.Value
                : SessionService?.AssessmentId ?? 0;

        Logger.LogInformation(
            "AssessmentAssetsPage resolved assessment {AssessmentId}",
            AssessmentId);
    }

    private void SelectTab(AssetPageTab tab)
    {
        ActiveTab = tab;
    }

    private void OnSessionChanged()
    {
        _ = InvokeAsync(() =>
        {
            ResolveAssessmentContext();
            StateHasChanged();
        });
    }

    public void Dispose()
    {
        if (SessionService != null)
        {
            SessionService.OnSessionChanged -= OnSessionChanged;
        }
    }

    private enum AssetPageTab
    {
        Summary,
        Details
    }
}
