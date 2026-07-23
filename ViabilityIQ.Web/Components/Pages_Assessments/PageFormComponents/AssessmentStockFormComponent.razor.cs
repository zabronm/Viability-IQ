using Microsoft.AspNetCore.Components;
using ViabilityIQ.Application.Interfaces;
using ViabilityIQ.Infrastructure.Repositories;
using ViabilityIQ.Shared.DataModels;
using ViabilityIQ.Shared.SharedModels;
using ViabilityIQ.Web.Services;

namespace ViabilityIQ.Web.Components.Pages_Assessments
{
    public partial class AssessmentStockFormComponent : ComponentBase
    {       
        [Inject] IGenericDataRepository<AssessmentStock> DataRepository { get; set; } = default!;
        [Inject] MasterDataService? ViqCrudService { get; set; }
        [Inject] ZabOffCanvasService? zabCanvasService { get; set; }
        [Inject] ISessionService? sessionService { get; set; }

        [Parameter] public long AssessmentId { get; set; }
        [Parameter] public long AssessmentStockId { get; set; }

        private AssessmentStock FormModel = new();
        private decimal[] MonthlyValues { get; set; } = new decimal[12];
        private decimal BulkTargetValue { get; set; }
        private bool IsSubmitting { get; set; }

        protected override async Task OnParametersSetAsync()
        {
            await LoadStock();
        }   


        //===== Load the stock record ===================
        async Task LoadStock()
        {
            try
            {
                if (AssessmentStockId > 0)
                {
                    FormModel = await ViqCrudService!.GetSingleAsync<AssessmentStock>("tblAssessmentStock", new { AssessmentStockId })
                                ?? new AssessmentStock {AssessmentId= AssessmentId };
                    MonthlyValues = FormModel.MonthlyValues;            // Load existing monthly data from the entity
                }
                else
                {
                    FormModel = new AssessmentStock {AssessmentId = AssessmentId, blIncludeVAT = true, };
                    MonthlyValues = new decimal[12];
                }
            }
            catch (Exception ex)
            {
                throw;
            }        
        }



        private void DistributeStockValuesEvenly()
        {
            for (int i = 0; i < 12; i++) MonthlyValues[i] = BulkTargetValue;
        }

        private async Task ExecuteSaveWorkflowAsync()
        {
            if (IsSubmitting) return;

            try
            {
                IsSubmitting = true;
                // Sync UI array to Entity array               
                FormModel.MonthlyValues = MonthlyValues;

                await DataRepository.SaveAsync(FormModel);
                await zabCanvasService!.PublishResultAsync(SaveResult.SavedAndClose("Stock details saved successfully."));
            }
            catch (Exception ex)
            {
                await zabCanvasService!.PublishResultAsync(new SaveResult { Success = false, Message = $"Error: {ex.Message}" });
            }
            finally
            {
                IsSubmitting = false;
            }
        }

        private async Task CancelFormAsync() => await zabCanvasService!.HideAsync(SaveResult.Cancel());
    }
}