using Microsoft.AspNetCore.Components;
using ViabilityIQ.Shared.DataModels.FinCalculations;
using ViabilityIQ.Shared.DataModels;
using System.Linq;


namespace ViabilityIQ.Web.Components.Pages_Assessments.CommonComponents
{
    public partial class NetProfitMonthlyGraphComponent
    {
        [Parameter] public AssessmentFinancialsDto Data { get; set; } = new();

        private decimal MaxVal => new[] {
        Data.MonthlySales.Max(),
        Data.MonthlyCostOfSales.Max(),
        Data.MonthlyExpenses.Max()
    }.Max();

        private double GetY(decimal val)
        {
            if (MaxVal == 0) return 150;
            return 150 - (double)(val / MaxVal * 140m);
        }

        private string GetLinePath(decimal[] source)
        {
            if (source == null || source.Length == 0) return string.Empty;
            return string.Join(" ", source.Select((val, i) => $"{(i == 0 ? "M" : "L")} {45 + (i * 46)} {GetY(val)}"));
        }
    }
}
