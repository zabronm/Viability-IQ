using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ViabilityIQ.Web.Services;

namespace ViabilityIQ.Web.Components.Pages_Assessments
{
    // MOVE ENUMS AND TYPES OUT OF THE PARTIAL CLASS LAYER SO BLAZOR ENGINE CAN DISCOVER THEM UNCONDITIONAL
    public enum WorkflowContextType { AssessmentLoanView, RepaymentsEdit }

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

    public partial class AssessmentLoanRepaymentsPage
    {
        [Inject] ZabOffCanvasService zabCanvasService { get; set; } = default!;
        [Parameter] public long AssessmentId { get; set; }

        private string SearchQuery { get; set; } = string.Empty;
        private string ActivePanelTitle { get; set; } = "Loan Administration Module";
        private long SelectedLoanId { get; set; }
        private WorkflowContextType ActiveWorkflowContext { get; set; } = WorkflowContextType.RepaymentsEdit;

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
            return ".." + name.Substring(0, 18);
        }

        private IEnumerable<LoanRepaymentRowViewModel> GetFilteredLoanProfiles()
        {
            if (string.IsNullOrWhiteSpace(SearchQuery)) return LoanProfilesDataset;

            return LoanProfilesDataset.Where(x =>
                x.LoanTypeName.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase) ||
                x.BankName.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase));
        }

        private async Task HandleGridActionBindingAsync(long targetLoanId, WorkflowContextType targetedWorkflow)
        {
            SelectedLoanId = targetLoanId;
            ActiveWorkflowContext = targetedWorkflow;
            ActivePanelTitle = targetedWorkflow == WorkflowContextType.AssessmentLoanView
                ? "Assessment Loan Configuration Blueprint"
                : "Modify Scheduled Operational Repayments";

            await zabCanvasService.ShowAsync(
    new CanvasRequest
    {
        Title = ActivePanelTitle,

        Width = 460,

        ComponentType = typeof(AssessmentLoanRepaymentFormComponent),

        Parameters = new Dictionary<string, object?>
        {
            ["LoanId"] = SelectedLoanId,

            ["WorkflowContext"] = ActiveWorkflowContext,

            ["OnSaveComplete"] = EventCallback.Factory.Create<RepaymentFormViewModel>(this, ProcessExecutionFeedback),

            ["OnCancel"] = EventCallback.Factory.Create(this, CloseEditorAsync)
        }
    });
        }

        private async Task ProcessExecutionFeedback(RepaymentFormViewModel updatedModel)
        {
            var matchedLoan = LoanProfilesDataset.FirstOrDefault(x => x.LoanId == updatedModel.LoanId);
            if (matchedLoan != null)
            {
                matchedLoan.StartMonth = updatedModel.StartMonth;
                matchedLoan.MonthlyData = updatedModel.MonthlyLines.Select(x => new RepaymentMetricCell
                {
                    Expected = x.Expected,
                    Interest = x.Interest,
                    Extra = x.Extra
                }).ToList();
            }
        }

        private async Task CloseEditorAsync()
        {
            await zabCanvasService.CloseAsync();
        }


    }
}