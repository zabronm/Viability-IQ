using Microsoft.AspNetCore.Components;
using System.Linq;
using System.Threading.Tasks;
using ViabilityIQ.Web.Services;

namespace ViabilityIQ.Web.Components.Pages_Assessments
{
    public partial class AssessmentLoanRepaymentFormComponent : ComponentBase
    {
        [Inject]        public ZabOffCanvasService zabCanvasService { get; set; } = default!;
        [Parameter]        public long LoanId { get; set; } 
        //[Parameter]        public WorkflowContextType WorkflowContext { get; set; }

        private RepaymentFormViewModel ActiveRepaymentModel { get; set; } = new();

        protected override void OnParametersSet()
        {
            LoadIsolatedFormModel();
        }

        private void LoadIsolatedFormModel()
        {
            //if (WorkflowContext != WorkflowContextType.RepaymentsEdit)
                //return;

            LoanId = 301;

            string targetedName =
                LoanId == 301
                    ? "Commercial Office Facility Industrial Branch"
                    : "Core Datacenter Azure Tech Cluster Infrastructure";

            int targetStartMonth =
                LoanId == 301 ? 5 : 3;

            ActiveRepaymentModel = new RepaymentFormViewModel
            {
                LoanId = LoanId,
                LoanTypeName = targetedName,
                StartMonth = targetStartMonth,
                SendToCashbook = true,

                MonthlyLines =
                    Enumerable.Range(0, 12)
                    .Select(i => new RepaymentMetricCell
                    {
                        Expected = LoanId == 301 ? 14000 : 8500,

                        Interest =
                            LoanId == 301
                                ? (i >= 4 ? 3200 : 2500)
                                : 1900,

                        Extra =
                            (LoanId == 301 && i == 8)
                                ? 8500
                                : 0
                    })
                    .ToList()
            };
        }

        private async Task SubmitFormWorkflowAsync()
        {
            // TODO:
            // Later this will become:
            //
            // SaveResult result =
            //      await LoanRepository.SaveAsync(ActiveRepaymentModel);
            //
            // await zabCanvasService.CloseAsync(result);

            await zabCanvasService.CloseAsync();
        }

        private async Task CancelFormWorkflowAsync()
        {
            await zabCanvasService.CloseAsync();
        }
    }
}