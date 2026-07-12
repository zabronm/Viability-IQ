using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ViabilityIQ.Application.Interfaces;
using ViabilityIQ.Shared.DataModels;
using ViabilityIQ.Shared.SharedModels;
using ViabilityIQ.Web.Components.CommonComponents;
using ViabilityIQ.Web.Components.Pages_Assessments.PageFormComponents;
using ViabilityIQ.Web.Services;
using static ViabilityIQ.Web.Components.CommonComponents.ViqAlertComponent;

namespace ViabilityIQ.Web.Components.Pages_Assessments
{
    // MOVE ENUMS AND TYPES OUT OF THE PARTIAL CLASS LAYER SO BLAZOR ENGINE CAN DISCOVER THEM UNCONDITIONAL
    //public enum WorkflowContextType { AssessmentLoanView, RepaymentsEdit }
       

    public partial class AssessmentLoanRepaymentsPage
    {

        [Inject] ToastService _Toast { get; set; } = default!;
        [Inject] ZabOffCanvasService zabCanvasService { get; set; } = default!;
        [Inject] ISessionService? sessionService { get; set; }
        [Parameter] public long AssessmentId { get; set; }

        //====================== ALERT NOTIFICATION VARIABLES ==============
        private bool blAlert { get; set; } = true;
        private AlertSeverity AlertSeverity { get; set; } = AlertSeverity.Warning;
        private string AlertHeading { get; set; } = "Inventory Notice:";
        private string AlertMessage { get; set; } = "Verify that your closing stock values align accurately with your cost of sales allocations for this assessment phase.";


        private string SearchQuery { get; set; } = string.Empty;
        private string ActivePanelTitle { get; set; } = "Loan Administration Module";
        private long SelectedLoanId { get; set; }
        //private WorkflowContextType ActiveWorkflowContext { get; set; } = WorkflowContextType.RepaymentsEdit;

        private List<LoanRepaymentRowViewModel> LoanProfilesDataset { get; set; } = new();

        protected override void OnInitialized()
        {            
            SeedRepaymentsPipelineSnapshot();
        }

        private void SeedRepaymentsPipelineSnapshot()
        {
            LoanProfilesDataset = new List<LoanRepaymentRowViewModel>
            {
                new LoanRepaymentRowViewModel
                {
                    LoanId = 301,
                    LoanTypeName = "Commercial Office Facility Industrial Branch",
                    BankName = "Nedbank Corporate CIB",
                    StartMonth = 5,
                    MonthlyData = Enumerable.Range(0, 12).Select(i => new RepaymentMetricCell { Expected = 14000, Interest = i >= 4 ? 3200 : 2500, Extra = i == 8 ? 8500 : 0 }).ToList()
                },
                new LoanRepaymentRowViewModel
                {
                    LoanId = 302,
                    LoanTypeName = "Core Datacenter Azure Tech Cluster Infrastructure",
                    BankName = "Standard Bank South Africa",
                    StartMonth = 3,
                    MonthlyData = Enumerable.Range(0, 12).Select(i => new RepaymentMetricCell { Expected = 8500, Interest = 1900, Extra = 0 }).ToList()
                }
            };
        }

        private string FormatLoanName(string name)
        {
            if (string.IsNullOrEmpty(name)) return string.Empty;
            if (name.Length <= 20) return name;
            return name.Substring(0, 18) + ".." ;
        }

        private IEnumerable<LoanRepaymentRowViewModel> GetFilteredLoanProfiles()
        {
            if (string.IsNullOrWhiteSpace(SearchQuery)) return LoanProfilesDataset;

            return LoanProfilesDataset.Where(x =>
                x.LoanTypeName.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase) ||
                x.BankName.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase));
        }


        //===================================== METHOD CALLING OFFCANVAS ===============
        private async Task OpenLoanFormAsync(long assessmentLoanId)
        {
            try
            {
                ActivePanelTitle = assessmentLoanId == 0 ?
                    "Add Assessment Loan" : "Edit Assessment Loan";

                await zabCanvasService.ShowAsync(
                    new CanvasRequest
                    {
                        Title = ActivePanelTitle,
                        Width = 360,
                        ComponentType = typeof(AssessmentLoanFormComponent),

                        Parameters = new
                        {
                            AssessmentLoanId = assessmentLoanId,
                            //LoanId = AssessmentId
                        },
                        ResultCallback = HandleNewLoanResultAsync
                    });
            }
            catch (Exception ex)
            {
                throw;
            }
            finally { }
        }


        //================ OPEN THE LOAN REPAYMENTS FORM  ==========================
        private async Task OpenRepaymentsFormAsync(long LoanRepaymentId)
        {
            try
            {
                ActivePanelTitle = LoanRepaymentId == 0 ?
                    "Add Loan Repayment" : "Edit Loan Repayment";

                await zabCanvasService.ShowAsync(
                    new CanvasRequest
                    {
                        Title = ActivePanelTitle,
                        Width = 420,
                        ComponentType = typeof(AssessmentLoanRepaymentFormComponent),
                        Parameters = new
                        {
                            LoanId = LoanRepaymentId
                        },
 
                        ResultCallback = HandleLoanRepaymentResultAsync                 //Handle results from component
                    });

            }
            catch (Exception ex)
            {
                throw;
            }
            finally
            {

            }  
        }


        private async Task HandleNewLoanResultAsync(SaveResult result)
        {
            if (result.Success)
            {
                // Refresh the loan profiles dataset or perform any necessary actions
                SeedRepaymentsPipelineSnapshot();
                StateHasChanged();

                _Toast.ShowSuccess(result.Message, sessionService!.AppTitle);

                await Task.CompletedTask;
            }

            if (!result.Success)
            {
                // Refresh the loan profiles dataset or perform any necessary actions
                _Toast.ShowError(result.Message, sessionService!.AppTitle);
                await Task.CompletedTask;
            }

            if (!result.Cancelled)
            {
                // Refresh the loan profiles dataset or perform any necessary actions
                _Toast.ShowInfo("You aborted the operation", sessionService!.AppTitle);
                await Task.CompletedTask;
            }
        }

        // Calculates vertical aggregated repayment column value per monthly bucket offset[cite: 6]
        private decimal GetMonthlyGrandTotal(int monthIndex)
        {
            if (LoanProfilesDataset == null || !LoanProfilesDataset.Any()) return 0m;

            return LoanProfilesDataset.Sum(loan =>
                loan.MonthlyData.Count > monthIndex ? loan.MonthlyData[monthIndex].Total : 0m);
        }

        // Calculates ultimate compound layout lifecycle index sum across all data matrices[cite: 6]
        private decimal GetAccumulatedGrandTotal()
        {
            if (LoanProfilesDataset == null || !LoanProfilesDataset.Any()) return 0m;

            return LoanProfilesDataset.Sum(loan => loan.MonthlyData.Sum(x => x.Total));
        }

        private async Task HandleLoanRepaymentResultAsync(SaveResult result)
        {            
            if (result.Success)
            {
                // Refresh the loan profiles dataset or perform any necessary actions
                SeedRepaymentsPipelineSnapshot();
                StateHasChanged();

                _Toast.ShowSuccess(result.Message, sessionService!.AppTitle);

                await Task.CompletedTask;
            }

            if (!result.Success)
            {
                // Refresh the loan profiles dataset or perform any necessary actions
                _Toast.ShowError(result.Message, sessionService!.AppTitle);
                await Task.CompletedTask;
            }

            if (!result.Cancelled)
            {
                // Refresh the loan profiles dataset or perform any necessary actions
                _Toast.ShowInfo("You aborted the operation", sessionService!.AppTitle);
                await Task.CompletedTask;
            }
        }

    }


    public class LoanRepaymentRowViewModel
    {
        public long LoanId { get; set; }
        public string LoanTypeName { get; set; }
        public string BankName { get; set; }
        public int StartMonth { get; set; }
        public List<RepaymentMetricCell> MonthlyData { get; set; } = new();
    }

    public class RepaymentFormViewModel
    {
        public long LoanId { get; set; }
        public string LoanTypeName { get; set; }
        public int StartMonth { get; set; }
        public bool SendToCashbook { get; set; }
        public List<RepaymentMetricCell> MonthlyLines { get; set; } = new();
    }

    public class RepaymentMetricCell
    {
        public decimal Expected { get; set; }
        public decimal Interest { get; set; }
        public decimal Extra { get; set; }
        public decimal Total => Expected + Interest + Extra;
    }
}