using Microsoft.AspNetCore.Components;
using ViabilityIQ.Application.Interfaces;
using ViabilityIQ.Shared.DataModels;
using ViabilityIQ.Shared.SharedModels;

namespace ViabilityIQ.Web.Components.Pages_Assessments.PageFormComponents
{
    public partial class SalesCategoryFormNewComponent 
    {
        [Parameter] public long AssessmentId { get; set; }
        [Parameter] public long AssessmentSalesCategoryId { get; set; }       

        [Inject] private IGenericDataRepository<AssessmentSalesCategory> DataRepository { get; set; } = default!;

        private SaveResult result { get; set; } = new();
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
                        result = new SaveResult
                        {
                            Success = false,
                            ClosePanel = false,
                            Message = $"Error: Sales category targeting reference key #{AssessmentSalesCategoryId} missing."
                        };
                    }
                }
                else
                {
                    // Mode B: CREATE mode clean canvas allocation blueprint initialization
                    Model = new AssessmentSalesCategory
                    {
                        AssessmentSalesCategoryId = 0, // Signals repo to trigger an automated SQL INSERT statement
                        AssessmentId = AssessmentId,
                        AssessmentSalesCategoryName = string.Empty,                        
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
                result = new SaveResult
                {
                    Success = false,
                    ClosePanel = false,
                    Message = $"Initialization infrastructure error mapping context: {ex.Message}"
                };
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
            if (string.IsNullOrWhiteSpace(Model.AssessmentSalesCategoryName))
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

                result = new SaveResult
                {
                    Success = executionFeedbackPackage.Success,
                    ClosePanel = executionFeedbackPackage.ClosePanel,
                    Message = executionFeedbackPackage.Message
                };
            }
            catch (Exception ex)
            {
                result = new SaveResult
                {
                    Success = false,
                    ClosePanel = false,
                    Message = $"Critical database interaction exception logging data payload streams: {ex.Message}"
                };
            }
            finally
            {
                IsSubmitting = false;
            }
        }
    }
}