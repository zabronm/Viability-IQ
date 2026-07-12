using Microsoft.AspNetCore.Components;
using ViabilityIQ.Shared.DataModels;
using ViabilityIQ.Shared.SharedModels;

namespace ViabilityIQ.Web.Components.Pages_Assessments.CommonComponents
{
    public partial class ConsolidatedRevenueAnalysisComponent: ComponentBase
    {
        [Parameter] public List<UnifiedIncomeViewModel> IncomeStreams { get; set; } = new();
        private string GetStreamMonthlySum(IncomeTypeEnum type, int index)
        {
            var total = IncomeStreams.Where(x => x.Type == type).Sum(c => c.MonthlyValues[index] * (c.IncludesVat ? 1.15m : 1.00m));
            return total.ToString("N0").Replace(",", " ");
        }

        private string GetStreamAnnualSum(IncomeTypeEnum type)
        {
            var total = IncomeStreams.Where(x => x.Type == type).Sum(c => c.MonthlyValues.Sum() * (c.IncludesVat ? 1.15m : 1.00m));
            return total.ToString("N0").Replace(",", " ");
        }

        private string GetCombinedTotalMonthlySum(int index)
        {
            var total = IncomeStreams.Sum(c => c.MonthlyValues[index] * (c.IncludesVat ? 1.15m : 1.00m));
            return total.ToString("N0").Replace(",", " ");
        }

        private string GetCombinedTotalAnnualSum()
        {
            var total = IncomeStreams.Sum(c => c.MonthlyValues.Sum() * (c.IncludesVat ? 1.15m : 1.00m));
            return total.ToString("N0").Replace(",", " ");
        }
    }
}

