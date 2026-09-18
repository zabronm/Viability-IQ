namespace ViabilityIQ.Web.Components.Reporting;

public sealed record EmailReportSubmitRequest(string Recipient, string Subject, string Message);
