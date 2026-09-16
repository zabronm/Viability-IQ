namespace ViabilityIQ.Shared.FinancialModels;

public sealed class MonthlyVatProjection
{
    public int Period { get; init; }
    public string MonthLabel => $"M{Period}";
    public decimal TaxableSales { get; init; }
    public decimal TaxablePurchases { get; init; }
    public decimal OutputVat { get; init; }
    public decimal StockInputVat { get; init; }
    public decimal ExpenseInputVat { get; init; }
    public decimal AdjustmentOutputVat { get; init; }
    public decimal AdjustmentInputVat { get; init; }
    public decimal InputVat => StockInputVat + ExpenseInputVat + AdjustmentInputVat;
    public decimal VatPayable => OutputVat + AdjustmentOutputVat - InputVat;
}
