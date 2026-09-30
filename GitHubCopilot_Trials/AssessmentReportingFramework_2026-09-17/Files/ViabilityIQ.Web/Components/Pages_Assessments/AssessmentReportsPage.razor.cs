using System.Net;
using System.Net.Mail;
using System.Text;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using ViabilityIQ.Application.Interfaces;
using ViabilityIQ.Shared.Reporting;
using ViabilityIQ.Shared.SharedModels;
using ViabilityIQ.Web.Components.Reporting;

namespace ViabilityIQ.Web.Components.Pages_Assessments;

public partial class AssessmentReportsPage
{
    [Parameter] public long AssessmentId { get; set; }
    [Inject] private IAssessmentReportService Reports { get; set; } = default!;
    [Inject] private IReportWorkbookWriter Workbooks { get; set; } = default!;
    [Inject] private IReportActivityRepository Activities { get; set; } = default!;
    [Inject] private IEmailReportingService Email { get; set; } = default!;
    [Inject] private ISessionService Session { get; set; } = default!;
    [Inject] private IJSRuntime JS { get; set; } = default!;
    [Inject] private ILogger<AssessmentReportsPage> Logger { get; set; } = default!;

    private ReportType SelectedType { get; set; } = ReportType.AssessmentSummary;
    private ReportDocument? Document { get; set; }
    private int StartMonth { get; set; } = 1;
    private int EndMonth { get; set; } = 12;
    private bool IsLoading { get; set; }
    private bool IsPdfBusy { get; set; }
    private bool IsExcelBusy { get; set; }
    private bool IsEmailBusy { get; set; }
    private bool ShowEmail { get; set; }
    private bool EmailSucceeded { get; set; }
    private string? EmailStatus { get; set; }
    private string? ErrorMessage { get; set; }
    private string? AuditWarning { get; set; }
    private long _loadedAssessmentId;
    private string ReportElementId => $"assessment-report-{AssessmentId}";

    protected override async Task OnParametersSetAsync()
    {
        if (_loadedAssessmentId == AssessmentId) return;
        _loadedAssessmentId = AssessmentId;
        await GenerateAsync();
    }

    private async Task SelectReportAsync(ReportType type)
    {
        SelectedType = type;
        await GenerateAsync();
    }

    private async Task GenerateAsync()
    {
        if (StartMonth > EndMonth)
        {
            ErrorMessage = "The start month must not be after the end month.";
            return;
        }
        IsLoading = true;
        ErrorMessage = null;
        AuditWarning = null;
        try
        {
            Document = await Reports.BuildAsync(AssessmentId, SelectedType, StartMonth, EndMonth);
            await AuditAsync(ReportActivityType.Generated, null, true, null, null);
        }
        catch (Exception exception)
        {
            Document = null;
            ErrorMessage = "The report could not be generated. Verify the assessment baseline and try again.";
            Logger.LogError(exception, "Report generation failed for assessment {AssessmentId}", AssessmentId);
        }
        finally { IsLoading = false; }
    }

    private async Task ExportPdfAsync()
    {
        if (Document is null) return;
        IsPdfBusy = true;
        try
        {
            await JS.InvokeVoidAsync("viqReportOutput.printElement",
                $"#{ReportElementId}", Document.Definition.Name, Document.Definition.Landscape);
            await AuditAsync(ReportActivityType.PdfExported, ReportOutputFormat.Pdf, true, null, null);
        }
        catch (Exception exception)
        {
            ErrorMessage = "The PDF print preview could not be opened.";
            Logger.LogError(exception, "Report print failed for assessment {AssessmentId}", AssessmentId);
            await AuditAsync(ReportActivityType.PdfExported, ReportOutputFormat.Pdf, false, "ClientPrint", null);
        }
        finally { IsPdfBusy = false; }
    }

    private async Task ExportExcelAsync()
    {
        if (Document is null) return;
        IsExcelBusy = true;
        try
        {
            var bytes = Workbooks.Write(Document);
            await JS.InvokeVoidAsync("viqReportOutput.downloadBase64", FileName(Document),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                Convert.ToBase64String(bytes));
            await AuditAsync(ReportActivityType.ExcelExported, ReportOutputFormat.Excel, true, null, null);
        }
        catch (Exception exception)
        {
            ErrorMessage = "The Excel workbook could not be created.";
            Logger.LogError(exception, "Report Excel export failed for assessment {AssessmentId}", AssessmentId);
            await AuditAsync(ReportActivityType.ExcelExported, ReportOutputFormat.Excel, false, "Workbook", null);
        }
        finally { IsExcelBusy = false; }
    }

    private Task OpenEmail()
    {
        EmailStatus = null;
        EmailSucceeded = false;
        ShowEmail = true;
        return Task.CompletedTask;
    }

    private Task CloseEmail()
    {
        if (!IsEmailBusy) ShowEmail = false;
        return Task.CompletedTask;
    }

    private async Task SendEmailAsync(EmailReportSubmitRequest request)
    {
        if (Document is null) return;
        IsEmailBusy = true;
        EmailStatus = null;
        EmailSucceeded = false;
        string? maskedRecipient = null;
        try
        {
            var address = new MailAddress(request.Recipient);
            maskedRecipient = Mask(address.Address);
            var bytes = Workbooks.Write(Document);
            var result = await Email.SendReportAsync(new EmailReportRequest
            {
                RecipientAddress = address.Address,
                SubjectTitle = request.Subject,
                MessageBodyText = BuildHtmlBody(request.Message, Document),
                AttachmentBytes = bytes,
                AttachmentName = FileName(Document),
                AttachmentContentType =
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                AttachmentFormat = "Excel"
            });
            EmailSucceeded = result.Succeeded;
            EmailStatus = result.Succeeded
                ? $"Email sent. Reference: {result.Reference}"
                : $"{result.ErrorMessage} Reference: {result.Reference}";
            await AuditAsync(result.Succeeded ? ReportActivityType.EmailSent : ReportActivityType.EmailFailed,
                ReportOutputFormat.Excel, result.Succeeded, result.ErrorCategory, maskedRecipient);
        }
        catch (FormatException)
        {
            EmailStatus = "Enter a valid recipient email address.";
            await AuditAsync(ReportActivityType.EmailFailed, ReportOutputFormat.Excel,
                false, "Validation", maskedRecipient);
        }
        catch (Exception exception)
        {
            EmailStatus = "The email could not be prepared.";
            Logger.LogError(exception, "Report email preparation failed for assessment {AssessmentId}", AssessmentId);
            await AuditAsync(ReportActivityType.EmailFailed, ReportOutputFormat.Excel,
                false, "Preparation", maskedRecipient);
        }
        finally { IsEmailBusy = false; }
    }

    private async Task AuditAsync(
        ReportActivityType activityType, ReportOutputFormat? format, bool succeeded,
        string? errorCategory, string? recipient)
    {
        if (Document is null) return;
        var result = await Activities.RecordAsync(new ReportActivity(
            Document.Definition.Scope, Document.Definition.Code, Document.Definition.Name,
            Document.Definition.Version, AssessmentId,
            Session.IsAuthenticated ? Session.UserId.ToString() : null,
            activityType, format, recipient, succeeded, errorCategory,
            Guid.NewGuid(), DateTime.UtcNow));
        if (!result.Recorded) AuditWarning = result.Error;
    }

    private static string FileName(ReportDocument document) =>
        $"{document.Definition.Code}_{document.AssessmentId}_{document.GeneratedAtUtc:yyyyMMdd_HHmm}.xlsx";
    private static string Mask(string address)
    {
        var parts = address.Split('@', 2);
        var local = parts[0];
        return $"{local[..Math.Min(2, local.Length)]}***@{parts[1]}";
    }
    private static string BuildHtmlBody(string message, ReportDocument document)
    {
        var safeMessage = WebUtility.HtmlEncode(message).Replace(Environment.NewLine, "<br/>");
        var metrics = document.Sections.SelectMany(x => x.Metrics).Take(12);
        var rows = string.Join("", metrics.Select(x =>
            $"<tr><td style='padding:6px;border-bottom:1px solid #ddd'>{WebUtility.HtmlEncode(x.Label)}</td>" +
            $"<td style='padding:6px;border-bottom:1px solid #ddd;text-align:right'><b>{WebUtility.HtmlEncode(x.DisplayValue)}</b></td></tr>"));
        return $"<div style='font-family:Segoe UI,Arial;color:#263f51'><h2>{WebUtility.HtmlEncode(document.Definition.Name)}</h2>" +
            $"<p>{safeMessage}</p><p><b>{WebUtility.HtmlEncode(document.EntityName ?? document.AssessmentReference)}</b><br/>" +
            $"{WebUtility.HtmlEncode(document.ReportingPeriod)} · {WebUtility.HtmlEncode(document.ReadinessStatus)}</p>" +
            $"<table style='border-collapse:collapse'>{rows}</table><p style='font-size:11px;color:#667'>Generated by ViabilityIQ. The attached .xlsx contains the complete report.</p></div>";
    }

    private static string IconFor(ReportType type) => type switch
    {
        ReportType.AssessmentSummary => "bi bi-journal-richtext",
        ReportType.ProfitAndLoss => "bi bi-graph-up-arrow",
        ReportType.Cashflow => "bi bi-cash-stack",
        ReportType.BalanceSheet => "bi bi-columns-gap",
        _ => "bi bi-file-earmark"
    };
}
