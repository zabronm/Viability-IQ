using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using ViabilityIQ.Application.Interfaces;
using ViabilityIQ.Infrastructure.Repositories;
using ViabilityIQ.Shared.DataModels;
using ViabilityIQ.Shared.SharedModels;
using ViabilityIQ.Web.Services;

namespace ViabilityIQ.Web.Components.Pages_Assessments.PageFormComponents
{
    public partial class AssessmentStockFormComponent : ComponentBase
    {
        #region Injected Dependencies

        [Inject] private IGenericDataRepository<AssessmentStock> DataRepository { get; set; } = default!;
        [Inject] private MasterDataService? ViqCrudService { get; set; }
        [Inject] private ZabOffCanvasService? zabCanvasService { get; set; }
        [Inject] private ISessionService? sessionService { get; set; }
        [Inject] private IProjectionStateManager? projectionStateManager { get; set; }
        [Inject] private ILogger<AssessmentStockFormComponent>? Logger { get; set; }

        #endregion

        #region Parameters

        [Parameter] public long AssessmentId { get; set; }
        [Parameter] public long AssessmentStockId { get; set; }

        #endregion

        #region Private Fields

        private AssessmentStock FormModel = new();
        private decimal[] MonthlyValues { get; set; } = new decimal[12];
        private decimal BulkTargetValue { get; set; }
        private bool IsSubmitting { get; set; }

        #endregion

        #region Lifecycle Methods

        protected override async Task OnParametersSetAsync()
        {
            try
            {
                await LoadStock();
            }
            catch (Exception ex)
            {
                Logger?.LogError(ex, "Error loading stock for AssessmentStockId {AssessmentStockId}", AssessmentStockId);
                throw;
            }
        }

        #endregion

        #region Private Methods

        private async Task LoadStock()
        {
            try
            {
                Logger?.LogDebug("Loading stock data for AssessmentStockId {AssessmentStockId}", AssessmentStockId);

                if (AssessmentStockId > 0)
                {
                    FormModel = await ViqCrudService!.GetSingleAsync<AssessmentStock>("tblAssessmentStock", new { AssessmentStockId })
                                ?? new AssessmentStock { AssessmentId = AssessmentId };
                    MonthlyValues = FormModel.MonthlyValues;
                }
                else
                {
                    FormModel = new AssessmentStock { AssessmentId = AssessmentId, blIncludeVAT = true };
                    MonthlyValues = new decimal[12];
                }

                Logger?.LogDebug("Stock loaded successfully for AssessmentStockId {AssessmentStockId}", AssessmentStockId);
            }
            catch (Exception ex)
            {
                Logger?.LogError(ex, "Error loading stock for AssessmentStockId {AssessmentStockId}", AssessmentStockId);
                throw;
            }
        }

        private void DistributeStockValuesEvenly()
        {
            Logger?.LogDebug("Distributing stock values evenly: {Amount}", BulkTargetValue);
            for (int i = 0; i < 12; i++) MonthlyValues[i] = BulkTargetValue;
        }


        //===================  save monthly stock figures ==========================
        private async Task ExecuteSaveWorkflowAsync()
        {
            if (IsSubmitting) return;

            try
            {
                IsSubmitting = true;
                FormModel.MonthlyValues = MonthlyValues;

                Logger?.LogInformation("Saving stock for assessment {AssessmentId}", FormModel.AssessmentId);

                await DataRepository.SaveAsync(FormModel);

                // ✅ TRIGGER CASHFLOW RECALCULATION
                Logger?.LogInformation("Invalidating cashflow after stock save for assessment {AssessmentId}", FormModel.AssessmentId);

                await projectionStateManager!.InvalidateDataAsync("stock", FormModel.AssessmentId, FormModel.AssessmentId);

                await zabCanvasService!.PublishResultAsync(SaveResult.SavedAndClose("Stock details saved successfully."));
            }
            catch (Exception ex)
            {
                Logger?.LogError(ex, "Error saving stock for assessment {AssessmentId}", FormModel.AssessmentId);
                await zabCanvasService!.PublishResultAsync(new SaveResult { Success = false, Message = $"Error: {ex.Message}" });
            }
            finally
            {
                IsSubmitting = false;
            }
        }

        private async Task CancelFormAsync() => await zabCanvasService!.HideAsync(SaveResult.Cancel());

        #endregion
    }
}