using Microsoft.AspNetCore.Components;
using System;
using System.Threading.Tasks;

namespace ViabilityIQ.Web.Components.Pages_Assessments
{
    public partial class AssessmentStockFormComponent : ComponentBase
    {

        [Parameter] public StockManagementViewModel? StockContext { get; set; }
             
        private StockManagementViewModel FormModel { get; set; } = new();
        private decimal BulkStockValueTarget { get; set; }

        protected override void OnParametersSet()
        {
            if (StockContext != null)
            {
                // Create an isolated working clone copy of the array dataset 
                FormModel = new StockManagementViewModel
                {
                    StockRowLabel = StockContext.StockRowLabel,
                    MonthlyBalances = (decimal[])StockContext.MonthlyBalances.Clone()
                };
            }
        }

        private void DistributeStockValuesEvenly()
        {
            if (BulkStockValueTarget <= 0) return;
            for (int i = 0; i < 12; i++)
            {
                FormModel.MonthlyBalances[i] = Math.Round(BulkStockValueTarget, 0);
            }
            BulkStockValueTarget = 0;
        }

        private async Task SaveFormAsync()
        {
           
        }

        private async Task CancelFormAsync()
        {
           
        }
    }
}