using Microsoft.AspNetCore.Components;
using ViabilityIQ.Shared.FinancialModels;

namespace ViabilityIQ.Web.Components.Pages_Assessments;

public partial class AssessmentAccs_CreditorsPage
{
    [Parameter, EditorRequired]
    public AccountsProjectionResult Projection { get; set; } = default!;

    private decimal OpeningCreditors => Projection.Months.FirstOrDefault()?.OpeningCreditors ?? 0m;

    private decimal PaymentCoverage
    {
        get
        {
            var totalLiabilities = OpeningCreditors + Projection.TotalPurchases;
            return totalLiabilities <= 0m ? 0m : Projection.TotalPayments / totalLiabilities * 100m;
        }
    }

    private decimal ChartMaximum =>
        Math.Max(
            Projection.Months.Select(x => x.CreditorPayments).DefaultIfEmpty(0m).Max(),
            Projection.Months.Select(x => x.ClosingCreditors).DefaultIfEmpty(0m).Max());

    private static string Money(decimal value) => value.ToString("N0");
    private static decimal Width(decimal value) => Math.Clamp(value, 0m, 100m);

    private decimal BarHeight(decimal value) =>
        ChartMaximum <= 0m ? 0m : Math.Max(2m, value / ChartMaximum * 100m);

    private static string ChartTitle(MonthlyAccountsProjection month) =>
        $"M{month.MonthNumber}: payments {Money(month.CreditorPayments)}, closing creditors {Money(month.ClosingCreditors)}";
}
