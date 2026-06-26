using Microsoft.AspNetCore.Components;
using System.Collections.Generic;
using ViabilityIQ.Shared.SharedModels;
using ViabilityIQ.Web.Components.CommonComponents;

namespace ViabilityIQ.Web.Components.Pages_Assessments
{
    public partial class AssessmentSalesPage
    {
        [Parameter] public long AssessmentId { get; set; }
        [Parameter] public EventCallback<SaveResult> OnSaveComplete { get; set; }


        private ZabOffCanvas OffCanvasControlRef;
        private string ActivePanelTitle = string.Empty;
        private SalesCategoryViewModel SelectedCategoryContext;
        private decimal BulkAnnualValueTarget;

        private FormContextType ActiveFormType { get; set; } = FormContextType.Sales;

        // Reactive Evaluation Properties
        private decimal GrandTotalSales => SalesCategories?.Sum(c => c.MonthlySales.Sum()) ?? 0;
        private decimal GrandTotalExpenses => ExpenseCategories?.Sum(e => e.MonthlyExpenses.Sum()) ?? 0;

        private decimal SundryTotalPrincipal => SundryIncome?.MonthlyValues.Sum() ?? 0;
        private decimal SundryTotalInterestYield => Enumerable.Range(0, 12).Sum(i => CalculateMonthlyInterest(i));
        private decimal GrantsTotalFunding => GrantsDonations?.MonthlyAllocations.Sum() ?? 0;

        private decimal GrandTotalGlobalInflows => GrandTotalSales + SundryTotalPrincipal + SundryTotalInterestYield + GrantsTotalFunding;

        private List<SalesCategoryViewModel> SalesCategories = new()
        {
            new SalesCategoryViewModel { Id = 1, CategoryName = "Product Wholesale Supply Matrix Distribution Line", MarkupPercentage = 25.0m, IncludeVat = false, MonthlySales = new decimal[] { 45000, 48000, 52000, 51000, 49000, 55000, 58000, 62000, 60000, 64000, 71000, 85000 } },
            new SalesCategoryViewModel { Id = 2, CategoryName = "Direct Service Contracts Enterprise Level", MarkupPercentage = 45.5m, IncludeVat = true, MonthlySales = new decimal[] { 22000, 24000, 24000, 26000, 25000, 28000, 29000, 31000, 30000, 32000, 35000, 42000 } },
            new SalesCategoryViewModel { Id = 3, CategoryName = "Maintenance Subscriptions Rolling Monthly", MarkupPercentage = 65.0m, IncludeVat = false, MonthlySales = new decimal[] { 12000, 12500, 13000, 13200, 13500, 14000, 14200, 15000, 15500, 16000, 16800, 18500 } }
        };

        private List<ExpenseCategoryViewModel> ExpenseCategories = new()
        {
            new ExpenseCategoryViewModel { Id = 101, ExpenseName = "Cost of Sales / Stock Acquisitions", MonthlyExpenses = new decimal[] { 15000, 16000, 17500, 17000, 16200, 18000, 19000, 21000, 20000, 21500, 24000, 29000 } },
            new ExpenseCategoryViewModel { Id = 102, ExpenseName = "Operational Overheads & Headcount Remittances", MonthlyExpenses = new decimal[] { 12000, 12000, 12500, 12500, 13000, 13000, 13500, 13500, 14000, 14000, 15000, 16000 } }
        };

        private SundryIncomeViewModel SundryIncome = new()
        {
            IncomeName = "Property Sublet Revenue",
            AnnualInterestRate = 6.5m,
            MonthlyValues = new decimal[] { 5000, 5000, 5500, 5500, 5500, 6000, 6000, 6000, 6500, 6500, 7000, 7500 }
        };

        private GrantsDonationsViewModel GrantsDonations = new()
        {
            FundingName = "SADC Green Innovation Grant",
            MonthlyAllocations = new decimal[] { 20000, 0, 0, 20000, 0, 0, 25000, 0, 0, 30000, 0, 15000 }
        };

        // (3) 1-MONTH DEFERRED INTEREST REALIZATION ENGINE
        private decimal CalculateMonthlyInterest(int monthIndex)
        {
            // First month realization safeguard rule (Index 0 = Month 1)
            if (monthIndex == 0 || SundryIncome == null || SundryIncome.AnnualInterestRate <= 0)
                return 0;

            decimal principal = SundryIncome.MonthlyValues[monthIndex];
            return Math.Round(principal * (SundryIncome.AnnualInterestRate / 100m / 12m), 0);
        }

        private decimal CalculateTotalInflowsForMonth(int monthIndex)
        {
            decimal sales = SalesCategories.Sum(c => c.MonthlySales[monthIndex]);
            decimal sundry = SundryIncome?.MonthlyValues[monthIndex] ?? 0;
            decimal interest = CalculateMonthlyInterest(monthIndex);
            decimal grants = GrantsDonations?.MonthlyAllocations[monthIndex] ?? 0;
            return sales + sundry + interest + grants;
        }

        private string TruncateName(string value)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;
            return value.Length <= 20 ? value : $"{value.Substring(0, 17)}...";
        }

        private void ToggleVatExclusionState(SalesCategoryViewModel item, object isChecked)
        {
            bool targetValue = (bool)(isChecked ?? false);
            item.IncludeVat = targetValue;
            decimal vatMultiplier = targetValue ? 1.15m : (1.0m / 1.15m);

            for (int i = 0; i < item.MonthlySales.Length; i++)
            {
                item.MonthlySales[i] = Math.Round(item.MonthlySales[i] * vatMultiplier, 0);
            }
            StateHasChanged();
        }

        private void DistributeAnnualSalesEvenly()
        {
            if (SelectedCategoryContext != null && BulkAnnualValueTarget > 0)
            {
                decimal baseValue = Math.Round(BulkAnnualValueTarget / 12m, 0);
                for (int i = 0; i < 12; i++)
                {
                    SelectedCategoryContext.MonthlySales[i] = baseValue;
                }
                BulkAnnualValueTarget = 0;
            }
            StateHasChanged();
        }

        private void OpenSalesDataEntryPanel(SalesCategoryViewModel category)
        {
            ActiveFormType = FormContextType.Sales;
            SelectedCategoryContext = category;
            BulkAnnualValueTarget = 0;
            ActivePanelTitle = $"Modify Sales: {TruncateName(category.CategoryName)}";
            OffCanvasControlRef.OpenAsync();
        }

        private void OpenSundryEntryPanel()
        {
            ActiveFormType = FormContextType.Sundry;
            ActivePanelTitle = "Edit Sundry Revenue Parameters";
            OffCanvasControlRef.OpenAsync();
        }

        private void OpenGrantsEntryPanel()
        {
            ActiveFormType = FormContextType.Grants;
            ActivePanelTitle = "Edit Grants & Donations Tranches";
            OffCanvasControlRef.OpenAsync();
        }

        private void HandleWorkflowStateChanged()
        {
            OffCanvasControlRef.CloseAsync();
            StateHasChanged();
        }

        public enum FormContextType { Sales, Sundry, Grants }

        public class SalesCategoryViewModel
        {
            public long Id { get; set; }
            public string CategoryName { get; set; } = string.Empty;
            public decimal MarkupPercentage { get; set; }
            public bool IncludeVat { get; set; }
            public decimal[] MonthlySales { get; set; } = new decimal[12];
        }

        public class ExpenseCategoryViewModel
        {
            public long Id { get; set; }
            public string ExpenseName { get; set; } = string.Empty;
            public decimal[] MonthlyExpenses { get; set; } = new decimal[12];
        }

        public class SundryIncomeViewModel
        {
            public string IncomeName { get; set; } = string.Empty;
            public decimal AnnualInterestRate { get; set; }
            public decimal[] MonthlyValues { get; set; } = new decimal[12];
        }

        public class GrantsDonationsViewModel
        {
            public string FundingName { get; set; } = string.Empty;
            public decimal[] MonthlyAllocations { get; set; } = new decimal[12];
        }

        // ==========================================================================
        // DYNAMIC FINANCIAL RATIOS AND MANAGEMENT STATEMENT METRIC MATH ENGINES
        // ==========================================================================

        private decimal GetAnnualCostOfSales()
        {
            // Extracting Id 101 tracking COS records out of your framework categories structure
            var cosRow = ExpenseCategories?.FirstOrDefault(e => e.Id == 101);
            return cosRow?.MonthlyExpenses.Sum() ?? 0;
        }

        private decimal GetAnnualOperatingExpenses()
        {
            // Extracting Id 102 tracking standalone Overheads/OPEX out of your categories structure
            var opexRow = ExpenseCategories?.FirstOrDefault(e => e.Id == 102);
            return opexRow?.MonthlyExpenses.Sum() ?? 0;
        }

        private decimal GetAnnualGrossProfit()
        {
            return GrandTotalSales - GetAnnualCostOfSales();
        }

        private decimal GetAnnualGrossIncome()
        {
            return GetAnnualGrossProfit() + SundryTotalPrincipal + SundryTotalInterestYield;
        }

        private decimal GetAnnualNetProfit()
        {
            return GetAnnualGrossIncome() - GetAnnualOperatingExpenses();
        }

        private decimal CalculateBreakEvenSales()
        {
            decimal annualSales = GrandTotalSales;
            decimal annualCos = GetAnnualCostOfSales();
            decimal annualOpex = GetAnnualOperatingExpenses();

            if (annualSales == 0) return 0;

            // Contribution Margin Ratio = (Sales - Variable Costs) / Sales
            decimal contributionMarginRatio = (annualSales - annualCos) / annualSales;

            if (contributionMarginRatio == 0) return 0;

            // Break Even = Fixed Costs (OPEX) / Contribution Margin Ratio
            return Math.Round(annualOpex / contributionMarginRatio, 0);
        }

        private decimal CalculateMarginOfSafetyPercent()
        {
            decimal annualSales = GrandTotalSales;
            decimal breakEven = CalculateBreakEvenSales();

            if (annualSales == 0 || annualSales <= breakEven) return 0;

            return Math.Round(((annualSales - breakEven) / annualSales) * 100m, 1);
        }

        private decimal CalculateGrossProfitMarginPercent()
        {
            if (GrandTotalSales == 0) return 0;
            return Math.Round((GetAnnualGrossProfit() / GrandTotalSales) * 100m, 1);
        }

        private decimal CalculateOperatingLeverage()
        {
            decimal grossProfit = GetAnnualGrossProfit();
            decimal netProfit = GetAnnualNetProfit();

            if (netProfit <= 0) return 1.00m; // Protect against division by zero or negative baselines
            return Math.Round(grossProfit / netProfit, 2);
        }

        // ==========================================================================
        // COMPONENT RELATIVE SCALE VISUAL WIDTH CALCULATORS (BAR GRAPH ENGINE)
        // ==========================================================================

        private int GetExpenseChartPercentage() => 25;  // Hardcoded layout aspect width to align with chart reference index 1
        private int GetCosChartPercentage() => 12;      // Relative tracking scale ratio for cost of sales block representation
        private int GetNetProfitChartPercentage() => 63; // Main remainder allocation layout block mirroring image proportions


    }





}
