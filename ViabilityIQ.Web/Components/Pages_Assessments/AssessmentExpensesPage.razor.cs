using Microsoft.AspNetCore.Components;
using System.Collections.Generic;
using ViabilityIQ.Shared.SharedModels;
using ViabilityIQ.Web.Components.CommonComponents;
using static ViabilityIQ.Web.Components.CommonComponents.ViqAlertComponent;
using static ViabilityIQ.Web.Components.Pages_Assessments.AssessmentSalesPage;

namespace ViabilityIQ.Web.Components.Pages_Assessments
{
    public partial class AssessmentExpensesPage
    {
        [Parameter] public long AssessmentId { get; set; }
        [Parameter] public EventCallback<SaveResult> OnSaveComplete { get; set; }

        private bool blAlert { get; set; } = true;
        private AlertSeverity AlertSeverity { get; set; } = AlertSeverity.Warning;
        private string AlertHeading { get; set; } = "Sales Notice:";
        private string AlertMessage { get; set; } = "Verify that your sales values align accurately with your cost of sales allocations for this assessment phase.";


        private ZabOffCanvas OffCanvasControlRef;
        private string SearchQuery { get; set; } = string.Empty;
        private string ActivePanelTitle { get; set; } = string.Empty;
        private WizardMode ActiveWizardMode { get; set; } = WizardMode.SingleEntryOrEdit;

        private ExpenseEntryViewModel EditingExpenseContext { get; set; } = new();
        private decimal BulkAnnualValueTarget { get; set; }
        private decimal SalesPercentageTarget { get; set; }

        private List<ExpenseEntryViewModel> ExpenseWorkspaceCollection { get; set; } = new();
        private List<SetupSuggestionViewModel> BulkSetupCheckboxList { get; set; } = new();

        // =========================================================================
        // COMPONENT COHERENT MOCK REVENUE/SALES INTEGRATED DATA STRUCUTRES
        // =========================================================================
        private List<SalesCategoryViewModel> SalesCategories { get; set; } = new();
        private SundryIncomeViewModel SundryIncome { get; set; } = new();
        private GrantsDonationsViewModel GrantsDonations { get; set; } = new();

        private decimal GrandTotalSales => SalesCategories?.Sum(c => c.MonthlySales.Sum()) ?? 0;
        private decimal SundryTotalPrincipal => SundryIncome?.MonthlyValues.Sum() ?? 0;
        private decimal SundryTotalInterestYield => Enumerable.Range(0, 12).Sum(i => CalculateMonthlyInterest(i));
        private decimal GrantsTotalFunding => GrantsDonations?.MonthlyAllocations.Sum() ?? 0;
        private decimal GrandTotalGlobalInflows => GrandTotalSales + SundryTotalPrincipal + SundryTotalInterestYield + GrantsTotalFunding;

        // Bridge transformation property to safely present tracking state arrays to the extracted CashFlowSummaryDetails component
        private List<ExpenseCategoryViewModel> ConvertedExpenseCategoriesForSummary =>
            ExpenseWorkspaceCollection.Select(e => new ExpenseCategoryViewModel
            {
                Id = e.Id,
                ExpenseName = e.ExpenseName,
                MonthlyExpenses = e.MonthlyValues
            }).ToList();

        protected override void OnInitialized()
        {
            DisplayNotificationAlerts();
            LoadMockSetupExpensesSnapshot();
            InitializeSuggestedBulkMatrixPool();
            LoadCoherentMockRevenueStreams();
        }


        void DisplayNotificationAlerts()
        {
            if (GrandTotalSales > 90)
            {
                blAlert = true;
                AlertSeverity = AlertSeverity.Danger;
                AlertHeading = "Critical Turnover Slowdown:";
                AlertMessage = $"Inventory is holding for {SundryTotalPrincipal} days! This exceeds standard cash flow risk thresholds.";
            }
            else if (SundryTotalPrincipal <= 30)
            {
                blAlert = true;
                AlertSeverity = AlertSeverity.Success;
                AlertHeading = "Optimized Turnover Efficiency:";
                AlertMessage = "Excellent efficiency setup! Holding days indicate rapid product movement cycles.";
            }
            else
            {
                blAlert = true;
                AlertSeverity = AlertSeverity.Info;
                AlertHeading = "Information Ledger:";
                AlertMessage = "Inventory configurations are active. Adjust parameters using the edit action button framework at any time.";
            }
        }



        private IEnumerable<ExpenseEntryViewModel> FilteredExpenses =>
            string.IsNullOrWhiteSpace(SearchQuery)
                ? ExpenseWorkspaceCollection
                : ExpenseWorkspaceCollection.Where(e => e.ExpenseName.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase));

        private decimal CalculateAggregateMonthlyTotal(int monthIndex) => FilteredExpenses.Sum(e => e.MonthlyValues[monthIndex]);

        private decimal CalculateGlobalGrandTotalExpenses() => FilteredExpenses.Sum(e => e.MonthlyValues.Sum());

        private void ToggleExpenseCashbookState(ExpenseEntryViewModel expense, object checkedStateValue)
        {
            if (checkedStateValue is bool booleanState)
            {
                expense.AffectsCashbookDirectly = booleanState;
            }
        }

        private void ComputeAndDistributeSalesPercentageOutflows()
        {
            if (SalesPercentageTarget <= 0) return;

            decimal ratio = SalesPercentageTarget / 100m;
            var monthlyRevenueProfile = Enumerable.Range(0, 12).Select(i => SalesCategories.Sum(c => c.MonthlySales[i])).ToArray();

            for (int i = 0; i < 12; i++)
            {
                decimal calculatedValue = monthlyRevenueProfile[i] * ratio;
                EditingExpenseContext.MonthlyValues[i] = Math.Round(calculatedValue, 0);
            }
            SalesPercentageTarget = 0;
        }

        private void OpenNewExpensePanel()
        {
            ActiveWizardMode = WizardMode.SingleEntryOrEdit;
            ActivePanelTitle = "Create Custom Expense Row Context";
            BulkAnnualValueTarget = 0;
            SalesPercentageTarget = 0;
            EditingExpenseContext = new ExpenseEntryViewModel
            {
                Id = 0,
                MonthlyValues = new decimal[12],
                AffectsCashbookDirectly = true,
                Classification = ExpenseClassification.Operating
            };
            OffCanvasControlRef.OpenAsync();
        }

        private void OpenEditExpensePanel(ExpenseEntryViewModel expense)
        {
            ActiveWizardMode = WizardMode.SingleEntryOrEdit;
            ActivePanelTitle = $"Modify Outflow Metrics: {expense.ExpenseName}";
            BulkAnnualValueTarget = 0;
            SalesPercentageTarget = 0;

            EditingExpenseContext = new ExpenseEntryViewModel
            {
                Id = expense.Id,
                ExpenseName = expense.ExpenseName,
                Classification = expense.Classification,
                AffectsCashbookDirectly = expense.AffectsCashbookDirectly,
                MonthlyValues = (decimal[])expense.MonthlyValues.Clone()
            };
            OffCanvasControlRef.OpenAsync();
        }

        private void OpenBulkSetupPanel()
        {
            ActiveWizardMode = WizardMode.BulkSetupStep1;
            ActivePanelTitle = "Suggested Base Expense Framework Checklist Matrix";
            foreach (var item in BulkSetupCheckboxList)
            {
                item.IsSelected = ExpenseWorkspaceCollection.Any(e => e.ExpenseName.Equals(item.ExpenseName, StringComparison.OrdinalIgnoreCase));
            }
            OffCanvasControlRef.OpenAsync();
        }

        private void CommitBulkSelectionFrameworks()
        {
            foreach (var item in BulkSetupCheckboxList.Where(s => s.IsSelected))
            {
                if (!ExpenseWorkspaceCollection.Any(e => e.ExpenseName.Equals(item.ExpenseName, StringComparison.OrdinalIgnoreCase)))
                {
                    ExpenseWorkspaceCollection.Add(new ExpenseEntryViewModel
                    {
                        Id = DateTime.Now.Ticks + item.Id,
                        ExpenseName = item.ExpenseName,
                        Classification = item.Classification,
                        AffectsCashbookDirectly = true,
                        MonthlyValues = new decimal[12]
                    });
                }
            }
            CloseCanvasPanel();
        }

        private void SaveExpenseStateContext()
        {
            if (string.IsNullOrWhiteSpace(EditingExpenseContext.ExpenseName)) return;

            if (EditingExpenseContext.Id == 0)
            {
                EditingExpenseContext.Id = DateTime.Now.Ticks;
                ExpenseWorkspaceCollection.Add(EditingExpenseContext);
            }
            else
            {
                var originalMatch = ExpenseWorkspaceCollection.FirstOrDefault(e => e.Id == EditingExpenseContext.Id);
                if (originalMatch != null)
                {
                    originalMatch.ExpenseName = EditingExpenseContext.ExpenseName;
                    originalMatch.Classification = EditingExpenseContext.Classification;
                    originalMatch.AffectsCashbookDirectly = EditingExpenseContext.AffectsCashbookDirectly;
                    originalMatch.MonthlyValues = EditingExpenseContext.MonthlyValues;
                }
            }
            CloseCanvasPanel();
        }

        private void DistributeAnnualExpensesEvenly()
        {
            if (BulkAnnualValueTarget <= 0) return;
            decimal baseValue = Math.Round(BulkAnnualValueTarget / 12m, 0);
            for (int i = 0; i < 12; i++)
            {
                EditingExpenseContext.MonthlyValues[i] = baseValue;
            }
            BulkAnnualValueTarget = 0;
        }

        private void CloseCanvasPanel() => OffCanvasControlRef.CloseAsync();

        private string GetClassificationShortLabel(ExpenseClassification classification) => classification switch
        {
            ExpenseClassification.Capex => "CAPEX",
            ExpenseClassification.Operating => "Operating (OPEX)",
            ExpenseClassification.LoanInterest => "Loan Interest",
            ExpenseClassification.LoanCapital => "Loan Capital",
            _ => "General"
        };

        private string TruncateString(string sourceValue, int maximumAllowedCharacters)
        {
            if (string.IsNullOrEmpty(sourceValue)) return string.Empty;
            return sourceValue.Length <= maximumAllowedCharacters
                ? sourceValue
                : sourceValue.Substring(0, maximumAllowedCharacters) + "..";
        }

        // =========================================================================
        // FINANCIAL FORMULAS / RATIOS MATRIX COMPUTATIONS ENGINE
        // =========================================================================
        private decimal CalculateMonthlyInterest(int monthIndex)
        {
            if (monthIndex == 0 || SundryIncome == null || SundryIncome.AnnualInterestRate <= 0) return 0;
            return Math.Round(SundryIncome.MonthlyValues[monthIndex] * (SundryIncome.AnnualInterestRate / 100m / 12m), 0);
        }

        private decimal GetAnnualCostOfSales() => Math.Round(GrandTotalSales * 0.45m, 0); // COGS benchmark
        private decimal GetAnnualGrossProfit() => GrandTotalSales - GetAnnualCostOfSales();
        private decimal GetAnnualGrossIncome() => GetAnnualGrossProfit() + SundryTotalPrincipal + SundryTotalInterestYield;
        private decimal GetAnnualNetProfit() => GetAnnualGrossIncome() - CalculateGlobalGrandTotalExpenses();

        private decimal CalculateBreakEvenSales()
        {
            decimal grossProfit = GetAnnualGrossProfit();
            if (GrandTotalSales == 0 || grossProfit == 0) return 0;
            decimal contributionMarginRatio = grossProfit / GrandTotalSales;
            return Math.Round(CalculateGlobalGrandTotalExpenses() / contributionMarginRatio, 0);
        }

        private decimal CalculateMarginOfSafetyPercent()
        {
            decimal breakEven = CalculateBreakEvenSales();
            if (GrandTotalSales == 0 || GrandTotalSales <= breakEven) return 0;
            return Math.Round(((GrandTotalSales - breakEven) / GrandTotalSales) * 100m, 1);
        }

        private decimal CalculateGrossProfitMarginPercent() =>
            GrandTotalSales == 0 ? 0 : Math.Round((GetAnnualGrossProfit() / GrandTotalSales) * 100m, 1);

        private decimal CalculateOperatingLeverage()
        {
            decimal grossProfit = GetAnnualGrossProfit();
            decimal netProfit = GetAnnualNetProfit();
            return netProfit <= 0 ? 1.00m : Math.Round(grossProfit / netProfit, 2);
        }

        private int GetExpenseChartPercentage() => 28;
        private int GetCosChartPercentage() => 45;
        private int GetNetProfitChartPercentage() => 27;

        private void LoadMockSetupExpensesSnapshot()
        {
            ExpenseWorkspaceCollection = new List<ExpenseEntryViewModel>
            {
                new ExpenseEntryViewModel { Id = 1, ExpenseName = "Commercial Office Facilities Lease", Classification = ExpenseClassification.Operating, AffectsCashbookDirectly = true, MonthlyValues = new decimal[] { 14000, 14000, 14000, 14500, 14500, 14500, 14500, 14500, 14500, 16000, 16000, 16000 } },
                new ExpenseEntryViewModel { Id = 2, ExpenseName = "Core Datacenter Azure Compute Clusters", Classification = ExpenseClassification.Operating, AffectsCashbookDirectly = true, MonthlyValues = new decimal[] { 8500, 8900, 9200, 9100, 9500, 10200, 11000, 10800, 11500, 12000, 13400, 15000 } },
                new ExpenseEntryViewModel { Id = 3, ExpenseName = "Heavy Plant Industrial Machinery Asset Line 2B", Classification = ExpenseClassification.Capex, AffectsCashbookDirectly = false, MonthlyValues = new decimal[] { 125000, 0, 0, 0, 0, 0, 85000, 0, 0, 0, 0, 0 } },
                new ExpenseEntryViewModel { Id = 4, ExpenseName = "Standard Bank Primary Term Loan Tranche A", Classification = ExpenseClassification.LoanCapital, AffectsCashbookDirectly = true, MonthlyValues = new decimal[] { 5000, 5000, 5000, 5000, 5000, 5000, 5000, 5000, 5000, 5000, 5000, 5000 } }
            };
        }

        private void LoadCoherentMockRevenueStreams()
        {
            SalesCategories = new List<SalesCategoryViewModel>
            {
                new SalesCategoryViewModel { Id = 1, CategoryName = "Product Wholesale Supply Matrix Line", IncludeVat = false, MonthlySales = new decimal[] { 45000, 48000, 52000, 51000, 49000, 55000, 58000, 62000, 60000, 64000, 71000, 85000 } },
                new SalesCategoryViewModel { Id = 2, CategoryName = "E-Commerce Retail Direct Consumer Platform", IncludeVat = true, MonthlySales = new decimal[] { 18000, 19500, 22000, 21000, 23400, 26000, 27500, 29000, 28200, 31000, 35000, 44000 } }
            };

            SundryIncome = new SundryIncomeViewModel
            {
                IncomeName = "Corporate Liquidity Reserve Interest",
                AnnualInterestRate = 5.5m,
                MonthlyValues = new decimal[] { 250000, 250000, 255000, 255000, 260000, 260000, 260000, 265000, 265000, 270000, 270000, 275000 }
            };

            GrantsDonations = new GrantsDonationsViewModel
            {
                FundingName = "National Innovation Endowment Grant",
                MonthlyAllocations = new decimal[] { 0, 0, 50000, 0, 0, 0, 50000, 0, 0, 0, 0, 0 }
            };
        }

        private void InitializeSuggestedBulkMatrixPool()
        {
            BulkSetupCheckboxList = new List<SetupSuggestionViewModel>
            {
                new SetupSuggestionViewModel { Id = 101, ExpenseName = "Audit & Statutory Accounting Compliance Subscriptions", Classification = ExpenseClassification.Operating },
                new SetupSuggestionViewModel { Id = 102, ExpenseName = "Corporate Staff Medical Insurance Risk Allocations", Classification = ExpenseClassification.Operating },
                new SetupSuggestionViewModel { Id = 103, ExpenseName = "Marketing Campaigns & Localized Brand Placement Activities", Classification = ExpenseClassification.Operating }
            };
        }

        public enum ExpenseClassification { Capex, Operating, LoanInterest, LoanCapital }
        public enum WizardMode { BulkSetupStep1, SingleEntryOrEdit }

        public class ExpenseEntryViewModel
        {
            public long Id { get; set; }
            public string ExpenseName { get; set; } = string.Empty;
            public ExpenseClassification Classification { get; set; }
            public bool AffectsCashbookDirectly { get; set; }
            public decimal[] MonthlyValues { get; set; } = new decimal[12];
        }

        public class SetupSuggestionViewModel
        {
            public long Id { get; set; }
            public string ExpenseName { get; set; } = string.Empty;
            public ExpenseClassification Classification { get; set; }
            public bool IsSelected { get; set; }
        }


    }
}