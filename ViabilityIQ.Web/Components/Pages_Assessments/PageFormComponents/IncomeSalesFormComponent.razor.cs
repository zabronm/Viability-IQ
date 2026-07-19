using Microsoft.AspNetCore.Components;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using ViabilityIQ.Application.Interfaces;
using ViabilityIQ.Shared.DataModels;
using ViabilityIQ.Shared.SharedModels;
using ViabilityIQ.Shared.UtilityServices; // Added for Mapper
using ViabilityIQ.Web.Services;


namespace ViabilityIQ.Web.Components.Pages_Assessments.PageFormComponents
{
    public partial class IncomeSalesFormComponent : ComponentBase
    {
        [Inject] private ISessionService sessionService { get; set; } = default!;
        [Inject] private ZabOffCanvasService? zabCanvasService { get; set; }
        [Inject] private IGenericDataRepository<AssessmentSales> DataRepository { get; set; } = default!;

        [Parameter] public long AssessmentId { get; set; }
        [Parameter] public UnifiedIncomeViewModel? IncomeContext { get; set; }
        

        private AssessmentSales FormModel { get; set; } = new();
        private decimal[] MonthlyValues { get; set; } = new decimal[12];
        private decimal BulkAnnualValueTarget { get; set; }

        private decimal BaseTotalSum => MonthlyValues.Sum();
        private decimal GrandCalculatedTotalSum => BaseTotalSum * (FormModel.IncludeVAT > 0 ? 1.15m : 1.00m);


        private bool IsLoading { get; set; } = true;
        private bool IsSubmitting { get; set; } = false;



        protected override void OnParametersSet()
        {
            AssessmentId = sessionService.AssessmentId ?? 0;

            if (IncomeContext != null)
            {
                FormModel = new AssessmentSales
                {
                    AssessmentId = AssessmentId,
                    AssessmentSalesId = (int)IncomeContext.Id,
                    Description = IncomeContext.Description,
                    IncomeTypeId = IncomeContext.TypeId,
                    IncludeVAT = IncomeContext.IncludesVat ? 1 : 0
                };
                MonthlyValues = (decimal[])IncomeContext.MonthlyValues.Clone();
            }
            else
            {
                FormModel = new()
                {
                    AssessmentId = AssessmentId
                };               
            }
        }

        private void DistributeAnnualValueEvenly()
        {
            decimal slice = Math.Round(BulkAnnualValueTarget / 12m, 2);
            for (int i = 0; i < 12; i++) MonthlyValues[i] = slice;
        }

        private async Task ExecuteSaveWorkflowAsync()
        {
            SaveResult executionFeedbackPackage;

            if (FormModel == null || IsSubmitting) return;
            if (string.IsNullOrWhiteSpace(FormModel.Description))         //// Interface validation step guard check
            {
                return;
            }

            try
            {
                IsSubmitting = true;
                FormModel.MonthlyValues = MonthlyValues;
                FormModel.AssessmentId = AssessmentId;                

                bool isExecutionSuccess = await DataRepository.SaveAsync(FormModel);
                if (isExecutionSuccess)
                {
                    executionFeedbackPackage = new()
                    {
                        Success = isExecutionSuccess,
                        ClosePanel = isExecutionSuccess,
                        RefreshGrid= true,
                        Message = isExecutionSuccess
                            ? $"Monthly sales details for {FormModel.Description}  committed successfully."
                            : "Error encountered; monthly sales not saved, please retry."
                    };

                    //await OnSaveComplete.InvokeAsync(executionFeedbackPackage);
                    await zabCanvasService!.PublishResultAsync(executionFeedbackPackage);
                }
            }
            catch (Exception ex)           
            {
                await zabCanvasService!.PublishResultAsync(new SaveResult
                {
                    Success = false,
                    ClosePanel = false,
                    Message = $"Error encountered: {ex.Message}",
                    RefreshGrid = false,
                });
            }
            finally
            {

                IsSubmitting = false;
            }

            //await zabCanvasService!.HideAsync(SaveResult.SavedAndClose(FormModel, "Revenue entry updated successfully."));           
        }

        private async Task CancelFormAsync() => await zabCanvasService!.HideAsync(SaveResult.Cancel());
    }
}