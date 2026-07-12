using Microsoft.AspNetCore.Components;
using System.Net.NetworkInformation;
using ViabilityIQ.Shared.DataModels.FinCalculations;
using ViabilityIQ.Shared.DataModels;

namespace ViabilityIQ.Web.Components.Pages_Assessments.CommonComponents
{
    public partial class SalesRatiosComponent
    {
        [Parameter] public AssessmentFinancialsDto Data { get; set; } = new();

        private decimal TotalSales => Data.MonthlySales.Sum();
        private decimal TotalCos => Data.MonthlyCostOfSales.Sum();
        private decimal GrossMarginPercentage => TotalSales > 0 ? (TotalSales - TotalCos) / TotalSales : 0;
        private decimal BreakEvenSales => GrossMarginPercentage > 0 ? Data.TotalFixedCosts / GrossMarginPercentage : 0;
        private decimal MarginOfSafetyIndex => TotalSales > 0 && TotalSales >= BreakEvenSales
            ? ((TotalSales - BreakEvenSales) / TotalSales) * 100m
            : 0;
        private decimal AssetEfficiency => Data.TotalFixedAssets > 0 ? TotalSales / Data.TotalFixedAssets : 0;
        private decimal StockTurnover => Data.AverageStockValue > 0 ? TotalCos / Data.AverageStockValue : 0;
    }
}
