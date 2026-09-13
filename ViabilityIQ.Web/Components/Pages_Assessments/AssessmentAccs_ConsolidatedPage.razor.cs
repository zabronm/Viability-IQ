using Microsoft.AspNetCore.Components;
using ViabilityIQ.Shared.FinancialModels;

namespace ViabilityIQ.Web.Components.Pages_Assessments;

public partial class AssessmentAccs_ConsolidatedPage
{
    [Parameter, EditorRequired]
    public AccountsProjectionResult Projection { get; set; } = default!;

    private decimal NetCashMovement => Projection.TotalCollections - Projection.TotalPayments;

    private decimal ChartMaximum =>
        Math.Max(
            Projection.Months.Select(x => x.DebtorCollections).DefaultIfEmpty(0m).Max(),
            Projection.Months.Select(x => x.CreditorPayments).DefaultIfEmpty(0m).Max());

    private static string Money(decimal value) => value.ToString("N0");
    private static string ValueClass(decimal value) => value < 0m ? "text-danger" : "text-success";

    private decimal BarHeight(decimal value) =>
        ChartMaximum <= 0m ? 0m : Math.Max(2m, value / ChartMaximum * 100m);

    private static string ChartTitle(MonthlyAccountsProjection month) =>
        $"M{month.MonthNumber}: collections {Money(month.DebtorCollections)}, supplier payments {Money(month.CreditorPayments)}";
}
