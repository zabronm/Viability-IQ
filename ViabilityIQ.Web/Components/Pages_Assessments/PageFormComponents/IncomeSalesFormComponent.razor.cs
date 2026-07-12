using Microsoft.AspNetCore.Components;
using System;
using System.Linq;
using System.Threading.Tasks;
using ViabilityIQ.Shared.DataModels;
using ViabilityIQ.Shared.SharedModels;
using ViabilityIQ.Web.Services; // Ensure your custom service namespace is included

namespace ViabilityIQ.Web.Components.Pages_Assessments.PageFormComponents
{
    public partial class IncomeSalesFormComponent : ComponentBase
    {
        [Inject] private ZabOffCanvasService? zabCanvasService { get; set; }

        [Parameter] public UnifiedIncomeViewModel? IncomeContext { get; set; }

        private UnifiedIncomeViewModel FormModel { get; set; } = new();
        private decimal BulkAnnualValueTarget { get; set; }

        private decimal BaseTotalSum => FormModel?.MonthlyValues?.Sum() ?? 0;
        private decimal GrandCalculatedTotalSum => BaseTotalSum * (FormModel.IncludesVat ? 1.15m : 1.00m);

        protected override void OnParametersSet()
        {
            if (IncomeContext != null)
            {
                FormModel = new UnifiedIncomeViewModel
                {
                    Id = IncomeContext.Id,
                    Description = IncomeContext.Description,
                    Type = IncomeContext.Type,
                    IncludesVat = IncomeContext.IncludesVat,
                    MonthlyValues = (decimal[])IncomeContext.MonthlyValues.Clone()
                };
            }
        }

        private void DistributeAnnualValueEvenly()
        {
            if (BulkAnnualValueTarget <= 0) return;
            decimal standardizedMonthSlice = Math.Round(BulkAnnualValueTarget / 12m, 2);
            for (int i = 0; i < 12; i++)
            {
                FormModel.MonthlyValues[i] = standardizedMonthSlice;
            }
            BulkAnnualValueTarget = 0;
        }

        private async Task ExecuteSaveWorkflowAsync()
        {
            if (zabCanvasService != null)
            {
                // Cleanly pass your refactored SaveResult payload with the data back through the service layer
                await zabCanvasService.HideAsync(SaveResult.SavedAndClose(FormModel, "Revenue entry updated successfully."));
            }
        }

        private async Task CancelFormAsync()
        {
            if (zabCanvasService != null)
            {
                await zabCanvasService.HideAsync(SaveResult.Cancel());
            }
        }
    }
}