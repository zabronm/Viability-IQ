using Microsoft.AspNetCore.Components;
using ViabilityIQ.Shared.FinancialModels;

namespace ViabilityIQ.Web.Components.Pages_Assessments;

public partial class AssessmentAccs_DebtorsPage
{
    [Parameter, EditorRequired]
    public AccountsProjectionResult Projection { get; set; } = default!;

    private decimal OpeningDebtors => Projection.Months.FirstOrDefault()?.OpeningDebtors ?? 0m;

    private decimal DaysSalesOutstanding =>
        Projection.TotalSales <= 0m
            ? 0m
            : Projection.ClosingDebtors / (Projection.TotalSales / 365m);

    private decimal ChartMaximum =>
        Math.Max(
            Projection.Months.Select(x => x.DebtorCollections).DefaultIfEmpty(0m).Max(),
            Projection.Months.Select(x => x.ClosingDebtors).DefaultIfEmpty(0m).Max());

    private static string Money(decimal value) => value.ToString("N0");
    private static decimal Width(decimal value) => Math.Clamp(value, 0m, 100m);

    private decimal BarHeight(decimal value) =>
        ChartMaximum <= 0m ? 0m : Math.Max(2m, value / ChartMaximum * 100m);

    private static string ChartTitle(MonthlyAccountsProjection month) =>
        $"M{month.MonthNumber}: collections {Money(month.DebtorCollections)}, closing debtors {Money(month.ClosingDebtors)}";
}
