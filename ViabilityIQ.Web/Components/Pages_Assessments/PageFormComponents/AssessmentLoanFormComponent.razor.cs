using Microsoft.AspNetCore.Components;
using System.Globalization;
using ViabilityIQ.Application.Interfaces;
using ViabilityIQ.Infrastructure.Repositories;
using ViabilityIQ.Shared.DataModels;
using ViabilityIQ.Shared.SharedModels;
using ViabilityIQ.Web.Services;

namespace ViabilityIQ.Web.Components.Pages_Assessments.PageFormComponents
{
    public partial class AssessmentLoanFormComponent
    {

        [Inject] ZabOffCanvasService? zabOffCanvasService { get; set; }
        [Inject] ISessionService? sessionService { get; set; }
        [Inject] private IGenericDataRepository<AssessmentLoan> DataRepository { get; set; } = default!;
        [Inject] private IGenericDataRepository<AssessmentLoanRepayment>? loanRepaymentRepository { get; set; }
        [Inject] private MasterDataService? masterDataService { get; set; }
        [Inject] private IFinancialCalculationsEngine? financialEngine { get; set; } = default;                   // ====== CENTRAL HUB FOR ALL CALCULATES 

        [Parameter] public long AssessmentId { get; set; }
        [Parameter] public long AssessmentLoanId { get; set; }


        private AssessmentLoan? Model { get; set; }
        private AssessmentLoanRepayment? LoanRepaymentModel { get; set; }

        private SaveResult executionFeedbackPackage = new();

        private bool isExecutionSuccess = false;
        private bool isNewLoan { get; set; }
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
                        executionFeedbackPackage = new()
                        {
                            Success = false,
                            ClosePanel = false,
                            Message = $"Error: Loan targeting reference key #{AssessmentLoanId} missing."
                        };
                    }
                }
                else
                {
                    // Mode B: CREATE mode clean canvas allocation blueprint initialization
                    Model = new AssessmentLoan
                    {
                        AssessmentLoanId = 0, // Signals repo to trigger an automated SQL INSERT statement
                        AssessmentId = sessionService!.AssessmentId!.Value,
                        Active = true,
                        Remarks = string.Empty,
                        CreatedDate = DateTime.UtcNow,
                        ModifiedDate = DateTime.UtcNow
                    };
                }
            }
            catch (Exception ex)
            {
                executionFeedbackPackage = new()
                {
                    Success = false,
                    ClosePanel = false,
                    Message = $"Error encountered: {ex.Message}"
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

            try
            {
                IsSubmitting = true;
                isNewLoan = Model.AssessmentLoanId == 0 ? true : false;

                isExecutionSuccess = await DataRepository.SaveAsync(Model);

                if (isExecutionSuccess)
                {
                    if (!isNewLoan)
                    {
                        //============== DELETE (SET ACTIVE =FALSE) ==== UPDATE existing loan record, no need to update repayments
                        var str_sql = "UPDATE tblAssessmentLoanRepayment SET [Active]=@parActive WHERE (AssessmentLoanId = @parAssessmentLoanId)";
                        _ = masterDataService!.ExecuteCommandAsync(str_sql, new { parActive = false, parAssessmentLoanId = Model.AssessmentLoanId });
                    }


                    //============== write record into Loan repayments === call the financial engine 
                    var repaymentRecords = financialEngine!.BuildRepaymentRecords(Model, LoanCalculationMethodsEnums.ReducingBalance);
                    if (repaymentRecords != null)
                    {
                        foreach (var _record in repaymentRecords)
                        {
                            await loanRepaymentRepository!.SaveAsync(_record);
                        }
                    }

                    executionFeedbackPackage = SaveResult.SavedAndNew("Loan/repayment archived successfully.");     //NEW LOAN, NEW REPAYMENTS                    
                }
                else
                {
                    executionFeedbackPackage = SaveResult.Failed("Error encountered, could not save/update loan. Please retry,");
                }
            }
            catch (Exception ex)
            {
                executionFeedbackPackage = new SaveResult
                {
                    Success = false,
                    ClosePanel = false,
                    Message = $"Critical error encountered: {ex.Message}"
                };
            }
            finally
            {
                await zabOffCanvasService!.PublishResultAsync(executionFeedbackPackage);
                IsSubmitting = false;
            }
        }
    }
}