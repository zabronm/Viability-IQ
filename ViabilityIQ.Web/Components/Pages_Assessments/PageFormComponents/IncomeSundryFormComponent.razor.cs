using Microsoft.AspNetCore.Components;
using System;
using System.Threading.Tasks;
using static ViabilityIQ.Web.Components.Pages_Assessments.PageFormComponents.CashFlowSummaryDetailComponent;

namespace ViabilityIQ.Web.Components.Pages_Assessments.PageFormComponents
{
    public partial class IncomeSundryFormComponent : ComponentBase
    {
        [Parameter] public SundryIncomeViewModel? SundryContext { get; set; }       

        private SundryIncomeViewModel FormModel { get; set; } = new();
        private decimal BulkAnnualValueTarget { get; set; }

        protected override void OnParametersSet()
        {
            if (SundryContext != null)
            {
                FormModel = new SundryIncomeViewModel
                {
                    StreamId = SundryContext.StreamId,
                    StreamName = SundryContext.StreamName,
                    MonthlySundry = (decimal[])SundryContext.MonthlySundry.Clone()
                };
            }
        }

        private void DistributeAnnualValueEvenly()
        {
            if (BulkAnnualValueTarget <= 0) return;
            decimal standardizedMonthSlice = Math.Round(BulkAnnualValueTarget / 12m, 0);
            for (int i = 0; i < 12; i++)
            {
                FormModel.MonthlySundry[i] = standardizedMonthSlice;
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