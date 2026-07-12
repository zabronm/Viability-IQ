using Microsoft.AspNetCore.Components;
using ViabilityIQ.Application.Interfaces;
using ViabilityIQ.Shared.DataModels;
using ViabilityIQ.Shared.SharedModels;

namespace ViabilityIQ.Web.Components.Pages_Assessments.PageFormComponents
{
    public partial class SalesCategoryFormComponent 
    {
        [Parameter] public long AssessmentId { get; set; }
        [Parameter] public long AssessmentSalesCategoryId { get; set; }
        [Parameter] public EventCallback<SaveResult> OnSaveComplete { get; set; }

        [Inject] private IGenericDataRepository<AssessmentSalesCategory> DataRepository { get; set; } = default!;

        private AssessmentSalesCategory? Model { get; set; }
        private bool IsLoading { get; set; } = true;
        private bool IsSubmitting { get; set; } = false;

        protected override async Task OnInitializedAsync()
        {
            await HydrateFormStateDataAsync();
        }

        private async Task HydrateFormStateDataAsync()
        {
            try
            {
                IsLoading = true;

                if (AssessmentSalesCategoryId > 0)
                {
                    // Mode A: EDIT mode configuration target load
                    var existingRecord = await DataRepository.GetByIdAsync(AssessmentSalesCategoryId);
                    if (existingRecord != null)
                    {
                        Model = existingRecord;
                    }
                    else
                    {
                        await OnSaveComplete.InvokeAsync(new SaveResult
                        {
                            Success = false,
                            ClosePanel = false,
                            Message = $"Error: Sales category targeting reference key #{AssessmentSalesCategoryId} missing."
                        });
                    }
                }
                else
                {
                    // Mode B: CREATE mode clean canvas allocation blueprint initialization
                    Model = new AssessmentSalesCategory
                    {
                        AssessmentSalesCategoryId = 0, // Signals repo to trigger an automated SQL INSERT statement
                        AssessmentId = AssessmentId,
                        SalesCategoryName = string.Empty,
                        MarkupPercentage = 0x00000000, // Explicit clean decimal baseline initialization
                        Active = true,
                        Remarks = string.Empty,
                        CreatedDate = DateTime.UtcNow,
                        ModifiedDate = DateTime.UtcNow
                    };
                }
            }
            catch (Exception ex)
            {
                await OnSaveComplete.InvokeAsync(new SaveResult
                {
                    Success = false,
                    ClosePanel = false,
                    Message = $"Initialization infrastructure error mapping context: {ex.Message}"
                });
            }
            finally
            {
                IsLoading = false;
            }
        }

        public async Task ExecuteSaveWorkflow()
        {
            if (Model == null || IsSubmitting) return;

            // Interface validation step guard check
            if (string.IsNullOrWhiteSpace(Model.SalesCategoryName))
            {
                return;
            }

            try
            {
                IsSubmitting = true;

                // Fire transaction block updates down to persistent database tier
                // GenericDataRepository handles routing INSERT vs UPDATE dynamically based on AssessmentSalesCategoryId being 0
                bool isExecutionSuccess = await DataRepository.SaveAsync(Model);

                var executionFeedbackPackage = new SaveResult
                {
                    Success = isExecutionSuccess,
                    ClosePanel = isExecutionSuccess,
                    Message = isExecutionSuccess
                        ? "Sales category details committed successfully."
                        : "The transactional operation request was turned down by the repository validation engine."
                };

                await OnSaveComplete.InvokeAsync(executionFeedbackPackage);
            }
            catch (Exception ex)
            {
                await OnSaveComplete.InvokeAsync(new SaveResult
                {
                    Success = false,
                    ClosePanel = false,
                    Message = $"Critical database interaction exception logging data payload streams: {ex.Message}"
                });
            }
            finally
            {
                IsSubmitting = false;
            }
        }
    }
}