using Microsoft.AspNetCore.Components;
using System.Net.NetworkInformation;
using ViabilityIQ.Shared.DataModels.FinCalculations;
using ViabilityIQ.Shared.DataModels;


namespace ViabilityIQ.Web.Components.Pages_Assessments.CommonComponents
{
    public partial class NetProfitSummaryComponent
    {
        [Parameter] public AssessmentFinancialsDto Data { get; set; } = new();

        private decimal GetGrossIncome(int i) => Data.MonthlySales[i] - Data.MonthlyCostOfSales[i] + Data.MonthlySundryIncome[i];
        private decimal GetAnnualGrossIncome() => Data.MonthlySales.Sum() - Data.MonthlyCostOfSales.Sum() + Data.MonthlySundryIncome.Sum();
        private decimal GetNetProfit(int i) => GetGrossIncome(i) - Data.MonthlyExpenses[i];
        private decimal GetAnnualNetProfit() => GetAnnualGrossIncome() - Data.MonthlyExpenses.Sum();
    }
}
