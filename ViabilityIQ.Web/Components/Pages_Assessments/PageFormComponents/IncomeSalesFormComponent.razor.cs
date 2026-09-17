using Microsoft.AspNetCore.Components;
using ViabilityIQ.Application.Interfaces;
using ViabilityIQ.Shared.DataModels;
using ViabilityIQ.Shared.SharedModels;
using ViabilityIQ.Web.Services;

namespace ViabilityIQ.Web.Components.Pages_Assessments.PageFormComponents;

public partial class IncomeSalesFormComponent : ComponentBase
{
    [Inject] private ISessionService SessionService { get; set; } = default!;
    [Inject] private ZabOffCanvasService ZabCanvasService { get; set; } = default!;
    [Inject] private IGenericDataRepository<AssessmentSales> DataRepository { get; set; } = default!;
    [Inject] private ILogger<IncomeSalesFormComponent> Logger { get; set; } = default!;
    [Inject] private IProjectionStateManager ProjectionStateManager { get; set; } = default!;

    [Parameter] public long AssessmentId { get; set; }
    [Parameter] public UnifiedIncomeViewModel? IncomeContext { get; set; }

    private AssessmentSales FormModel { get; set; } = new();
    private decimal[] MonthlyValues { get; set; } = new decimal[12];
    private decimal BulkAnnualValueTarget { get; set; }
    private bool IsLoading { get; set; } = true;
    private bool IsSubmitting { get; set; }

    private decimal BaseTotalSum => MonthlyValues.Sum();
    private decimal GrandCalculatedTotalSum =>
        BaseTotalSum * VatFactor(FormModel.IncludeVAT, EffectiveVatRate);
    private decimal EffectiveVatRate =>
        FormModel.VATRate > 0m ? FormModel.VATRate : 15m;

    protected override async Task OnParametersSetAsync()
    {
        AssessmentId = SessionService.AssessmentId ?? AssessmentId;
        IsLoading = true;

        try
        {
            if (IncomeContext?.Id > 0)
            {
                var existing = await DataRepository.GetByIdAsync(IncomeContext.Id)
                    ?? throw new InvalidOperationException(
                        $"Revenue record {IncomeContext.Id} was not found.");
                if (existing.AssessmentId != AssessmentId)
                {
                    throw new InvalidOperationException(
                        "The selected revenue record belongs to a different assessment.");
                }

                FormModel = existing;
                MonthlyValues = (decimal[])existing.MonthlyValues.Clone();
            }
            else
            {
                FormModel = new AssessmentSales
                {
                    AssessmentId = AssessmentId,
                    Description = IncomeContext?.Description ?? string.Empty,
                    IncomeTypeId = IncomeContext?.TypeId ?? 1,
                    IncludeVAT = IncomeContext?.IncludesVat == true ? 1m : 0m,
                    Active = true
                };
                MonthlyValues = IncomeContext?.MonthlyValues?.Length == 12
                    ? (decimal[])IncomeContext.MonthlyValues.Clone()
                    : new decimal[12];
            }

            BulkAnnualValueTarget = MonthlyValues.Sum();
        }
        catch (Exception exception)
        {
            Logger.LogError(
                exception,
                "Unable to initialize revenue form for assessment {AssessmentId}",
                AssessmentId);
            await ZabCanvasService.PublishResultAsync(SaveResult.Failed(exception.Message));
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void DistributeAnnualValueEvenly()
    {
        var monthlyAmount = Math.Round(BulkAnnualValueTarget / 12m, 2);
        for (var index = 0; index < MonthlyValues.Length; index++)
        {
            MonthlyValues[index] = monthlyAmount;
        }
    }

    private async Task ExecuteSaveWorkflowAsync()
    {
        if (IsSubmitting || string.IsNullOrWhiteSpace(FormModel.Description))
        {
            return;
        }

        IsSubmitting = true;
        try
        {
            FormModel.AssessmentId = AssessmentId;
            FormModel.MonthlyValues = MonthlyValues;
            FormModel.TotalNoVAT = BaseTotalSum;
            FormModel.VATRate = FormModel.IncludeVAT > 0m ? EffectiveVatRate : 0m;
            FormModel.TotalWithVAT = GrandCalculatedTotalSum;

            var saved = await DataRepository.SaveAsync(FormModel);
            if (!saved)
            {
                await ZabCanvasService.PublishResultAsync(
                    SaveResult.Failed("Revenue details could not be saved."));
                return;
            }

            await ProjectionStateManager.InvalidateDataAsync(
                "Sales",
                FormModel.AssessmentSalesId,
                AssessmentId);

            await ZabCanvasService.PublishResultAsync(SaveResult.SavedAndClose(
                FormModel,
                $"Monthly sales details for {FormModel.Description} committed successfully."));
        }
        catch (Exception exception)
        {
            Logger.LogError(
                exception,
                "Error saving revenue data for assessment {AssessmentId}",
                AssessmentId);
            await ZabCanvasService.PublishResultAsync(
                SaveResult.Failed($"Revenue details could not be saved: {exception.Message}"));
        }
        finally
        {
            IsSubmitting = false;
        }
    }

    private async Task CancelFormAsync() =>
        await ZabCanvasService.HideAsync(SaveResult.Cancel());

    private static decimal VatFactor(decimal includeVat, decimal rate) =>
        includeVat > 0m ? 1m + Math.Max(rate, 0m) / 100m : 1m;
}
