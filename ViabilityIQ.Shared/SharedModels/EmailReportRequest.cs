namespace ViabilityIQ.Shared.SharedModels;

public sealed class EmailReportRequest
{
    public string RecipientAddress { get; set; } = string.Empty;
    public string SubjectTitle { get; set; } = string.Empty;
    public string MessageBodyText { get; set; } = string.Empty;
    public byte[]? AttachmentBytes { get; set; }
    public string AttachmentName { get; set; } = "Report_Export.xlsx";
    public string AttachmentContentType { get; set; } =
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
    public string AttachmentFormat { get; set; } = "Excel";
}

public sealed record EmailDeliveryResult(
    bool Succeeded, string Reference, string? ErrorCategory, string? ErrorMessage);

