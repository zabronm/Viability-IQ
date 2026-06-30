using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ViabilityIQ.Web.Components.Pages_Assessments
{
    public partial class AssessmentLoanRepaymentFormComponent : ComponentBase
    {
        [Parameter] public long LoanId { get; set; }
        [Parameter] public WorkflowContextType WorkflowContext { get; set; }
        [Parameter] public EventCallback<RepaymentFormViewModel> OnSaveComplete { get; set; }
        [Parameter] public EventCallback OnCancel { get; set; }

        private RepaymentFormViewModel ActiveRepaymentModel { get; set; }

        protected override void OnParametersSet()
        {
            LoadIsolatedFormModel();
        }

        private void LoadIsolatedFormModel()
        {
            if (WorkflowContext != WorkflowContextType.RepaymentsEdit) return;

            string targetedName = LoanId == 301
                ? "Commercial Office Facility Industrial Branch"
                : "Core Datacenter Azure Tech Cluster Infrastructure";

            int targetStartMonth = LoanId == 301 ? 5 : 3;

            ActiveRepaymentModel = new RepaymentFormViewModel
            {
                LoanId = LoanId,
                LoanTypeName = targetedName,
                StartMonth = targetStartMonth,
                SendToCashbook = true,
                MonthlyLines = Enumerable.Range(0, 12).Select(i => new RepaymentMetricCell
                {
                    Expected = LoanId == 301 ? 14000 : 8500,
                    Interest = LoanId == 301 ? (i >= 4 ? 3200 : 2500) : 1900,
                    Extra = (LoanId == 301 && i == 8) ? 8500 : 0
                }).ToList()
            };
        }

        private async Task SubmitFormWorkflowAsync()
        {
            if (OnSaveComplete.HasDelegate)
                await OnSaveComplete.InvokeAsync(ActiveRepaymentModel);

            await OnCancel.InvokeAsync();
        }

        private async Task CancelFormWorkflowAsync()
        {
            if (OnCancel.HasDelegate)
            {
                await OnCancel.InvokeAsync();
            }
        }
    }
}