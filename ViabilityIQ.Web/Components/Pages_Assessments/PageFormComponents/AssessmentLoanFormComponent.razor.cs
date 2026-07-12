using Microsoft.AspNetCore.Components;
using ViabilityIQ.Application.Interfaces;
using ViabilityIQ.Shared.DataModels;
using ViabilityIQ.Shared.SharedModels;

namespace ViabilityIQ.Web.Components.Pages_Assessments.PageFormComponents
{
    public partial class AssessmentLoanFormComponent 
    {
        [Parameter] public long AssessmentId { get; set; }
       
        [Parameter] public long AssessmentLoanId { get; set; }
       
        [Inject] private IGenericDataRepository<AssessmentLoan> DataRepository { get; set; } = default!;

        private AssessmentLoan? Model { get; set; }
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

                if (AssessmentLoanId > 0)
                {
                    // Mode A: EDIT mode configuration target load
                    var existingRecord = await DataRepository.GetByIdAsync(AssessmentLoanId);
                    if (existingRecord != null)
                    {
                        Model = existingRecord;
                    }
                    else
                    {
                        //await OnSaveComplete.InvokeAsync(new SaveResult
                        //{
                        //    Success = false,
                        //    ClosePanel = false,
                        //    Message = $"Error: Loan targeting reference key #{AssessmentLoanId} missing."
                        //});
                    }
                }
                else
                {
                    // Mode B: CREATE mode clean canvas allocation blueprint initialization
                    Model = new AssessmentLoan
                    {
                        AssessmentLoanId = 0, // Signals repo to trigger an automated SQL INSERT statement
                        AssessmentId = AssessmentId,                                           
                        Active = true,
                        Remarks = string.Empty,
                        CreatedDate = DateTime.UtcNow,
                        ModifiedDate = DateTime.UtcNow
                    };
                }
            }
            catch (Exception ex)
            {
                //await OnSaveComplete.InvokeAsync(new SaveResult
                //{
                //    Success = false,
                //    ClosePanel = false,
                //    Message = $"Error encountered: {ex.Message}"
                //});
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
            //if (string.IsNullOrWhiteSpace(Model.LoanAmount.ToString))
            //{
            //    return;
            //}

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
                        ? "Loan details committed successfully."
                        : "Error encountered while saving, please contact Administrator."
                };

                //await OnSaveComplete.InvokeAsync(executionFeedbackPackage);
            }
            catch (Exception ex)
            {
                //await OnSaveComplete.InvokeAsync(new SaveResult
                //{
                //    Success = false,
                //    ClosePanel = false,
                //    Message = $"Critical error encountered: {ex.Message}"
                //});
            }
            finally
            {
                IsSubmitting = false;
            }
        }
    }
}