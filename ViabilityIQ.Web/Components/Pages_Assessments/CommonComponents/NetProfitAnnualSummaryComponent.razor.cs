using Microsoft.AspNetCore.Components;
using System.Net.NetworkInformation;
using ViabilityIQ.Shared.DataModels.FinCalculations;
using ViabilityIQ.Shared.DataModels;


namespace ViabilityIQ.Web.Components.Pages_Assessments.CommonComponents
{
    public partial class NetProfitAnnualSummaryComponent
    {
        [Parameter] public AssessmentFinancialsDto Data { get; set; } = new();

        private decimal TotalSales => Data.MonthlySales.Sum();
        private decimal TotalCos => Data.MonthlyCostOfSales.Sum();
        private decimal TotalSundry => Data.MonthlySundryIncome.Sum();
        private decimal TotalExpenses => Data.MonthlyExpenses.Sum();
        private decimal GrossIncome => TotalSales - TotalCos + TotalSundry;
        private decimal NetProfit => GrossIncome - TotalExpenses;
    }
}
