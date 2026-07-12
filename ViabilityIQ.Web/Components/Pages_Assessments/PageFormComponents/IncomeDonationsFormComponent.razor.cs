using Microsoft.AspNetCore.Components;
using System;
using System.Threading.Tasks;
using static ViabilityIQ.Web.Components.Pages_Assessments.PageFormComponents.CashFlowSummaryDetailComponent;

namespace ViabilityIQ.Web.Components.Pages_Assessments.PageFormComponents
{
    public partial class IncomeDonationsFormComponent : ComponentBase
    {
        [Parameter] public GrantsDonationsViewModel? GrantsContext { get; set; }       

        private GrantsDonationsViewModel FormModel { get; set; } = new();
        private decimal BulkAnnualValueTarget { get; set; }

        protected override void OnParametersSet()
        {
            if (GrantsContext != null)
            {
                FormModel = new GrantsDonationsViewModel
                {
                    StreamId = GrantsContext.StreamId,
                    StreamLabel = GrantsContext.StreamLabel,
                    MonthlyAllocations = (decimal[])GrantsContext.MonthlyAllocations.Clone()
                };
            }
        }

        private void DistributeAnnualValueEvenly()
        {
            if (BulkAnnualValueTarget <= 0) return;
            decimal standardizedMonthSlice = Math.Round(BulkAnnualValueTarget / 12m, 0);
            for (int i = 0; i < 12; i++)
            {
                FormModel.MonthlyAllocations[i] = standardizedMonthSlice;
            }
            BulkAnnualValueTarget = 0;
        }

        private async Task ExecuteSaveWorkflowAsync()
        {
        }

        private async Task CancelFormAsync()
        {
            
        }
    }
}