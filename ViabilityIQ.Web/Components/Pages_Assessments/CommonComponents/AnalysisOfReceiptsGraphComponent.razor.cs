using Microsoft.AspNetCore.Components;
using System.Net.NetworkInformation;
using ViabilityIQ.Shared.DataModels.FinCalculations;
using ViabilityIQ.Shared.DataModels;


namespace ViabilityIQ.Web.Components.Pages_Assessments.CommonComponents
{
    public partial class AnalysisOfReceiptsGraphComponent
    {
        [Parameter] public AssessmentFinancialsDto Data { get; set; } = new();

        private decimal TotalCos => Data.MonthlyCostOfSales.Sum();
        private decimal TotalExp => Data.MonthlyExpenses.Sum();
        private decimal TotalProfit => Data.MonthlySales.Sum() - TotalCos + Data.MonthlySundryIncome.Sum() - TotalExp;

        private decimal TotalPool => TotalCos + TotalExp + (TotalProfit > 0 ? TotalProfit : 0);

        private double CosPct => TotalPool > 0 ? (double)(TotalCos / TotalPool * 100m) : 0;
        private double ExpPct => TotalPool > 0 ? (double)(TotalExp / TotalPool * 100m) : 0;
        private double ProfitPct => TotalPool > 0 ? (double)((TotalProfit > 0 ? TotalProfit : 0) / TotalPool * 100m) : 0;
    }
}
