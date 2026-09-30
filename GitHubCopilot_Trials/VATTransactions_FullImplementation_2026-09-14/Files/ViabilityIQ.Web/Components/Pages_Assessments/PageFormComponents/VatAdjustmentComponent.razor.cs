using Microsoft.AspNetCore.Components;
using ViabilityIQ.Application.Interfaces;
using ViabilityIQ.Shared.FinancialModels;
using ViabilityIQ.Shared.SharedModels;
using ViabilityIQ.Web.Services;

namespace ViabilityIQ.Web.Components.Pages_Assessments.PageFormComponents;

public partial class VatAdjustmentComponent : ComponentBase
{
    [Parameter]
    public long AssessmentId { get; set; }

    [Parameter]
    public EventCallback<SaveResult> OnSaved { get; set; }

    [Inject]
    private ToastService Toast { get; set; } = default!;

    [Inject]
    private ILogger<VatAdjustmentComponent> Logger { get; set; } = default!;

    [Inject]
    private IVatAdjustmentRepository AdjustmentRepository { get; set; } = default!;

    [Inject]
    private IProjectionStateManager ProjectionStateManager { get; set; } = default!;

    [Inject]
    private ISessionService SessionService { get; set; } = default!;

    private decimal[] outputAdjustments = new decimal[12];
    private decimal[] inputAdjustments = new decimal[12];
    private string[] monthNotes = new string[12];
    private string selectedReasonCode = string.Empty;
    private string auditJustification = string.Empty;
    private bool isSaving;
    private bool isLoading;
    private string successMessage = string.Empty;
    private string errorMessage = string.Empty;

    protected override async Task OnParametersSetAsync()
    {
        if (AssessmentId <= 0)
        {
            errorMessage = "A valid assessment is required.";
            return;
        }

        await LoadExistingAdjustments();
    }

    private async Task LoadExistingAdjustments()
    {
        isLoading = true;
        errorMessage = string.Empty;

        try
        {
            outputAdjustments = new decimal[12];
            inputAdjustments = new decimal[12];
            monthNotes = new string[12];

            var rows = await AdjustmentRepository.GetActiveAsync(AssessmentId);
            foreach (var row in rows.Where(x => x.Period is >= 1 and <= 12))
            {
                var index = row.Period - 1;
                outputAdjustments[index] = row.OutputVat;
                inputAdjustments[index] = row.InputVat;
                monthNotes[index] = row.Notes ?? string.Empty;
            }
        }
        catch (Exception exception)
        {
            Logger.LogError(
                exception,
                "Failed to load VAT adjustments for assessment {AssessmentId}",
                AssessmentId);
            errorMessage = "Existing VAT adjustments could not be loaded.";
        }
        finally
        {
            isLoading = false;
        }
    }

    private async Task HandleSave()
    {
        errorMessage = string.Empty;
        successMessage = string.Empty;

        if (string.IsNullOrWhiteSpace(auditJustification))
        {
            errorMessage = "Audit justification is required for VAT adjustments.";
            Toast.ShowError(errorMessage, "Validation Error");
            return;
        }

        isSaving = true;

        try
        {
            var months = Enumerable.Range(1, 12)
                .Select(period => new VatAdjustmentMonth(
                    period,
                    outputAdjustments[period - 1],
                    inputAdjustments[period - 1],
                    monthNotes[period - 1]))
                .ToList();

            await AdjustmentRepository.SaveAsync(new SaveVatAdjustmentsRequest(
                AssessmentId,
                selectedReasonCode,
                auditJustification,
                SessionService.UserId,
                months));

            await ProjectionStateManager.InvalidateDataAsync(
                "VAT",
                AssessmentId,
                AssessmentId);

            successMessage = "VAT adjustments saved successfully.";
            Toast.ShowSuccess(successMessage, "Success");
            await ReturnSuccess(successMessage);
        }
        catch (Exception exception)
        {
            Logger.LogError(
                exception,
                "Failed to save VAT adjustments for assessment {AssessmentId}",
                AssessmentId);
            errorMessage = "VAT adjustments could not be saved. No partial changes were committed.";
            Toast.ShowError(errorMessage, "Error");
        }
        finally
        {
            isSaving = false;
        }
    }

    private async Task HandleCancel()
    {
        await ReturnCancel();
    }

    public decimal GetTotalOutputAdjustments() => outputAdjustments.Sum();
    public decimal GetTotalInputAdjustments() => inputAdjustments.Sum();
    public decimal GetNetVatAdjustment() =>
        GetTotalOutputAdjustments() - GetTotalInputAdjustments();

    private async Task ReturnSuccess(string message)
    {
        if (OnSaved.HasDelegate)
        {
            await OnSaved.InvokeAsync(SaveResult.SavedAndClose(message));
        }
    }

    private async Task ReturnCancel()
    {
        if (OnSaved.HasDelegate)
        {
            await OnSaved.InvokeAsync(new SaveResult
            {
                Success = false,
                ClosePanel = true,
                Message = "VAT adjustment cancelled."
            });
        }
    }
}
