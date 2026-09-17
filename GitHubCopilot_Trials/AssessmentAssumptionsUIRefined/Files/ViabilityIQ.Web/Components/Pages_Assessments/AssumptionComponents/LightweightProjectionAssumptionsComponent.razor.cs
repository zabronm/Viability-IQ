using Microsoft.AspNetCore.Components;
using ViabilityIQ.Application.Interfaces;
using ViabilityIQ.Shared.DataModels;
using ViabilityIQ.Web.Services;

namespace ViabilityIQ.Web.Components.Pages_Assessments.AssumptionComponents;

public partial class LightweightProjectionAssumptionsComponent
{
    [Inject] private IGenericDataRepository<AssessmentProjectionAssumptions> Repository { get; set; } = default!;
    [Inject] private ToastService Toast { get; set; } = default!;
    [Inject] private ILogger<LightweightProjectionAssumptionsComponent> Logger { get; set; } = default!;

    [Parameter] public long AssessmentId { get; set; }
    [Parameter] public string Section { get; set; } = "sales";
    [Parameter] public EventCallback OnSaved { get; set; }

    private AssessmentProjectionAssumptions Model { get; set; } = new();
    private bool IsLoading { get; set; } = true;
    private bool IsSaving { get; set; }
    private string? ErrorMessage { get; set; }
    private List<string> ValidationMessages { get; set; } = [];
    private long _loadedAssessmentId;

    private string SectionTitle => Section switch
    {
        "sales" => "Sales and stock assumptions",
        "expenses" => "Expense and VAT assumptions",
        "funding" => "Cash reserve policy",
        _ => "Projection assumptions"
    };

    protected override async Task OnParametersSetAsync()
    {
        if (_loadedAssessmentId == AssessmentId)
            return;

        _loadedAssessmentId = AssessmentId;
        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        IsLoading = true;
        ErrorMessage = null;

        try
        {
            Model = (await Repository.GetAllAsync(item =>
                    item.AssessmentId == AssessmentId && item.Active))
                .OrderByDescending(item => item.AssessmentProjectionAssumptionsId)
                .FirstOrDefault()
                ?? new AssessmentProjectionAssumptions
                {
                    AssessmentId = AssessmentId,
                    Active = true,
                    SalesGrowthStartMonth = 1,
                    ExpenseIncreaseStartMonth = 1,
                    VatPaymentFrequencyMonths = 2
                };
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            Logger.LogError(ex, "Unable to load baseline assumptions for assessment {AssessmentId}", AssessmentId);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task SaveAsync()
    {
        ValidationMessages = Validate();
        if (ValidationMessages.Count > 0 || IsSaving)
            return;

        IsSaving = true;
        ErrorMessage = null;

        try
        {
            Model.AssessmentId = AssessmentId;
            Model.Active = true;
            Model.ModifiedDate = DateTime.UtcNow;
            Model.ConfirmedDate = Model.IsConfirmed ? DateTime.UtcNow : null;

            if (Model.AssessmentProjectionAssumptionsId == 0)
                Model.CreatedDate = DateTime.UtcNow;

            if (!await Repository.SaveAsync(Model))
                throw new InvalidOperationException("The assumptions record was not saved.");

            if (Model.AssessmentProjectionAssumptionsId == 0)
                await LoadAsync();

            Toast.ShowSuccess("Baseline assumptions saved.");
            await OnSaved.InvokeAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            Logger.LogError(ex, "Unable to save baseline assumptions for assessment {AssessmentId}", AssessmentId);
            Toast.ShowError("The baseline assumptions could not be saved.");
        }
        finally
        {
            IsSaving = false;
        }
    }

    private List<string> Validate()
    {
        var messages = new List<string>();

        if (Model.SalesGrowthStartMonth is < 1 or > 12)
            messages.Add("Sales growth start month must be M1 to M12.");

        if (Model.ExpenseIncreaseStartMonth is < 1 or > 12)
            messages.Add("Expense increase start month must be M1 to M12.");

        if (Model.CashSalesPercentage is < 0m or > 100m)
            messages.Add("Cash Sales must be between 0% and 100%.");

        if (Model.BadDebtRate is < 0m or > 100m)
            messages.Add("Bad debts must be between 0% and 100%.");

        if (Model.CostOfSalesPercentage is < 0m or > 100m)
            messages.Add("Cost of Sales must be between 0% and 100%.");

        if (Model.MinimumClosingStockPercentage is < 0m or > 100m)
            messages.Add("Minimum closing stock must be between 0% and 100%.");

        if (Model.ContingencyRate is < 0m or > 100m)
            messages.Add("Contingency must be between 0% and 100%.");

        if (Model.VatPaymentFrequencyMonths is not (1 or 2))
            messages.Add("VAT payment frequency must be monthly or every two months.");

        if (Model.MinimumCashBalance < 0m)
            messages.Add("Minimum cash balance cannot be negative.");

        return messages;
    }
}
