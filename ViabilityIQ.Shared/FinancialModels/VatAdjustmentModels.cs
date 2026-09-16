namespace ViabilityIQ.Shared.FinancialModels;

public sealed record VatAdjustmentMonth(
    int Period,
    decimal OutputVat,
    decimal InputVat,
    string? Notes);

public sealed record SaveVatAdjustmentsRequest(
    long AssessmentId,
    string ReasonCode,
    string AuditJustification,
    long UserId,
    IReadOnlyList<VatAdjustmentMonth> Months);
