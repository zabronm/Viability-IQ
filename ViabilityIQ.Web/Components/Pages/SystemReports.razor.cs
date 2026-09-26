using System.Net;
using System.Net.Mail;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using ViabilityIQ.Application.Interfaces;
using ViabilityIQ.Shared.Reporting;
using ViabilityIQ.Shared.SharedModels;
using ViabilityIQ.Web.Components.Reporting;

namespace ViabilityIQ.Web.Components.Pages;

public abstract class SystemReportsBase : ComponentBase, IAsyncDisposable
{
    [Inject] private IOperationalReportsService Reports { get; set; } = default!;
    [Inject] private ITenantAuthorizationService Authorization { get; set; } = default!;
    [Inject] private IPdfExportService Pdf { get; set; } = default!;
    [Inject] private IExcelEPPlusExportService Excel { get; set; } = default!;
    [Inject] private IEmailReportingService Email { get; set; } = default!;
    [Inject] protected ISessionService Session { get; set; } = default!;
    [Inject] private IJSRuntime JS { get; set; } = default!;
    [Inject] private ILogger<SystemReportsBase> Logger { get; set; } = default!;

    protected AssessmentOperationalReportFilter Filter { get; set; } = new();
    protected OperationalReportFilterOptions FilterOptions { get; set; } = new();
    protected IReadOnlyList<AssessmentOperationalReportRow> FilteredRows { get; set; } =
        Array.Empty<AssessmentOperationalReportRow>();
    protected OperationalReportType SelectedReport { get; set; } =
        OperationalReportType.AssessmentRegister;
    protected bool IsLoading { get; set; }
    protected bool IsPdfBusy { get; set; }
    protected bool IsExcelBusy { get; set; }
    protected bool IsEmailBusy { get; set; }
    protected bool ShowEmail { get; set; }
    protected bool EmailSucceeded { get; set; }
    protected string? EmailStatus { get; set; }
    protected string? ErrorMessage { get; set; }
    protected string? FilterError { get; set; }
    protected string? AccessDeniedMessage { get; set; }
    protected string AccessScopeLabel { get; set; } = "Authorised records";

    private CancellationTokenSource? _loadCancellation;

    protected static readonly IReadOnlyList<OperationalReportOption> StatusOptions =
    [
        new(1, "Draft"),
        new(2, "In Progress"),
        new(3, "Ready for Review"),
        new(4, "Completed"),
        new(5, "Archived")
    ];

    protected static readonly IReadOnlyList<ReportDefinition> ReportDefinitions =
    [
        new(
            OperationalReportType.AssessmentRegister,
            "Assessment Register",
            "Operational case listing with ownership, workflow and start dates.",
            "bi bi-clipboard2-data"),
        new(
            OperationalReportType.WorkflowReadiness,
            "Workflow & Readiness",
            "Monitor lifecycle status, readiness and stalled assessments.",
            "bi bi-speedometer2"),
        new(
            OperationalReportType.AdvisorPerformance,
            "Advisor Performance",
            "Compare assessment workload, completion and readiness by creator.",
            "bi bi-person-badge"),
        new(
            OperationalReportType.BranchPerformance,
            "Branch Performance",
            "Management roll-up of assessment delivery by branch.",
            "bi bi-diagram-3")
    ];

    protected ReportDefinition SelectedDefinition =>
        ReportDefinitions.First(report => report.Type == SelectedReport);

    protected int CompletedCount => FilteredRows.Count(row => row.StatusId == 4);
    protected int StalledCount => FilteredRows.Count(
        row => row.StatusId is 1 or 2 && row.DaysOpen >= 14);
    protected int ActiveAdvisorCount => FilteredRows.Select(row => row.AdvisorId).Distinct().Count();
    protected decimal AverageReadiness => FilteredRows.Count == 0
        ? 0
        : FilteredRows.Average(row => (decimal)row.ProgressPercentage);

    protected IReadOnlyList<WorkflowReadinessReportRow> WorkflowRows =>
        FilteredRows.Select(row => new WorkflowReadinessReportRow
        {
            CaseNumber = row.CaseNumber,
            BusinessName = row.BusinessName,
            StatusName = row.StatusName,
            ReadinessPercentage = row.ProgressPercentage,
            AdvisorName = row.AdvisorName,
            BranchName = row.BranchName,
            StartDate = row.AssessmentStartDate,
            LastUpdated = row.ModifiedDate,
            DaysOpen = row.DaysOpen
        }).ToList();

    protected IReadOnlyList<AdvisorPerformanceReportRow> AdvisorRows =>
        FilteredRows
            .GroupBy(row => new { row.AdvisorId, row.AdvisorName, row.BranchName })
            .Select(group => new AdvisorPerformanceReportRow
            {
                AdvisorName = group.Key.AdvisorName,
                BranchName = group.Key.BranchName,
                TotalAssessments = group.Count(),
                DraftCount = group.Count(row => row.StatusId == 1),
                InProgressCount = group.Count(row => row.StatusId == 2),
                ReadyForReviewCount = group.Count(row => row.StatusId == 3),
                CompletedCount = group.Count(row => row.StatusId == 4),
                AverageReadiness = group.Average(row => (decimal)row.ProgressPercentage),
                CompletionRate = Percentage(group.Count(row => row.StatusId == 4), group.Count())
            })
            .OrderByDescending(row => row.CompletionRate)
            .ThenBy(row => row.AdvisorName)
            .ToList();

    protected IReadOnlyList<BranchPerformanceReportRow> BranchRows =>
        FilteredRows
            .GroupBy(row => new { row.BranchId, row.BranchName })
            .Select(group => new BranchPerformanceReportRow
            {
                BranchName = group.Key.BranchName,
                AdvisorCount = group.Select(row => row.AdvisorId).Distinct().Count(),
                TotalAssessments = group.Count(),
                DraftCount = group.Count(row => row.StatusId == 1),
                InProgressCount = group.Count(row => row.StatusId == 2),
                ReadyForReviewCount = group.Count(row => row.StatusId == 3),
                CompletedCount = group.Count(row => row.StatusId == 4),
                AverageReadiness = group.Average(row => (decimal)row.ProgressPercentage),
                CompletionRate = Percentage(group.Count(row => row.StatusId == 4), group.Count())
            })
            .OrderByDescending(row => row.CompletionRate)
            .ThenBy(row => row.BranchName)
            .ToList();

    protected override async Task OnInitializedAsync()
    {
        try
        {
            var context = await Authorization.GetAccessContextAsync();
            if (!context.CanReadAllOperationalRecords && !context.CanReadScopedOperationalRecords)
            {
                AccessDeniedMessage = "Your tenant role does not permit operational reporting.";
                return;
            }

            AccessScopeLabel = context.CanReadAllOperationalRecords
                ? "All records in the active tenant"
                : "My records and explicitly granted assessments";

            FilterOptions = await Reports.GetAssessmentFilterOptionsAsync();
            await LoadReportAsync();
        }
        catch (UnauthorizedAccessException)
        {
            AccessDeniedMessage =
                "An active tenant membership with operational read access is required.";
        }
        catch (Exception exception)
        {
            ErrorMessage = "The reporting workspace could not be initialized.";
            Logger.LogError(exception, "Operational reports initialization failed.");
        }
    }

    protected void SelectReport(OperationalReportType reportType)
    {
        SelectedReport = reportType;
        EmailStatus = null;
    }

    protected async Task ResetFiltersAsync()
    {
        Filter = new AssessmentOperationalReportFilter();
        await LoadReportAsync();
    }

    protected async Task LoadReportAsync()
    {
        FilterError = null;
        ErrorMessage = null;
        EmailStatus = null;

        if (Filter.StartDateFrom.HasValue
            && Filter.StartDateTo.HasValue
            && Filter.StartDateFrom.Value.Date > Filter.StartDateTo.Value.Date)
        {
            FilterError = "The start-date-from value cannot be after the start-date-to value.";
            return;
        }

        await CancelCurrentLoadAsync();
        _loadCancellation = new CancellationTokenSource();
        IsLoading = true;
        try
        {
            FilteredRows = await Reports.GetAssessmentsAsync(
                Filter,
                _loadCancellation.Token);
        }
        catch (OperationCanceledException) when (_loadCancellation.IsCancellationRequested)
        {
        }
        catch (UnauthorizedAccessException)
        {
            FilteredRows = Array.Empty<AssessmentOperationalReportRow>();
            AccessDeniedMessage = "Your tenant role does not permit this report.";
        }
        catch (ArgumentException exception)
        {
            FilterError = exception.Message;
        }
        catch (Exception exception)
        {
            ErrorMessage = "The report data could not be loaded. Please retry.";
            Logger.LogError(exception, "Operational report data load failed.");
        }
        finally
        {
            IsLoading = false;
        }
    }

    protected async Task ExportPdfAsync()
    {
        if (FilteredRows.Count == 0) return;
        IsPdfBusy = true;
        ErrorMessage = null;
        try
        {
            var bytes = await GeneratePdfAsync();
            await DownloadAsync(bytes, BuildFileName("pdf"), "application/pdf");
        }
        catch (Exception exception)
        {
            ErrorMessage = "The PDF report could not be generated.";
            Logger.LogError(exception, "Operational PDF export failed.");
        }
        finally
        {
            IsPdfBusy = false;
        }
    }

    protected async Task ExportExcelAsync()
    {
        if (FilteredRows.Count == 0) return;
        IsExcelBusy = true;
        ErrorMessage = null;
        try
        {
            var bytes = await GenerateExcelAsync();
            await DownloadAsync(
                bytes,
                BuildFileName("xlsx"),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
        }
        catch (Exception exception)
        {
            ErrorMessage = "The Excel report could not be generated.";
            Logger.LogError(exception, "Operational Excel export failed.");
        }
        finally
        {
            IsExcelBusy = false;
        }
    }

    protected Task OpenEmail()
    {
        EmailStatus = null;
        EmailSucceeded = false;
        ShowEmail = true;
        return Task.CompletedTask;
    }

    protected Task CloseEmail()
    {
        if (!IsEmailBusy) ShowEmail = false;
        return Task.CompletedTask;
    }

    protected async Task SendEmailAsync(EmailReportSubmitRequest request)
    {
        if (FilteredRows.Count == 0) return;
        IsEmailBusy = true;
        EmailStatus = null;
        EmailSucceeded = false;
        try
        {
            var recipient = new MailAddress(request.Recipient);
            var bytes = await GenerateExcelAsync();
            var result = await Email.SendReportAsync(new EmailReportRequest
            {
                RecipientAddress = recipient.Address,
                SubjectTitle = request.Subject,
                MessageBodyText = BuildEmailBody(request.Message),
                AttachmentBytes = bytes,
                AttachmentName = BuildFileName("xlsx"),
                AttachmentContentType =
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                AttachmentFormat = "Excel"
            });

            EmailSucceeded = result.Succeeded;
            EmailStatus = result.Succeeded
                ? $"Email sent. Reference: {result.Reference}"
                : $"{result.ErrorMessage} Reference: {result.Reference}";
        }
        catch (FormatException)
        {
            EmailStatus = "Enter a valid recipient email address.";
        }
        catch (Exception exception)
        {
            EmailStatus = "The report email could not be prepared.";
            Logger.LogError(exception, "Operational report email preparation failed.");
        }
        finally
        {
            IsEmailBusy = false;
        }
    }

    protected static string FormatDate(DateTime? value) =>
        value?.ToString("dd MMM yyyy") ?? "Not set";

    protected static string StatusClass(long statusId) => statusId switch
    {
        1 => "opr-status--draft",
        2 => "opr-status--progress",
        3 => "opr-status--review",
        4 => "opr-status--complete",
        5 => "opr-status--archived",
        _ => string.Empty
    };

    protected static string StatusClassByName(string statusName) =>
        StatusClass(StatusOptions.FirstOrDefault(
            option => option.Name.Equals(statusName, StringComparison.OrdinalIgnoreCase))?.Id ?? 0);

    private Task<byte[]> GeneratePdfAsync() => SelectedReport switch
    {
        OperationalReportType.AssessmentRegister =>
            Pdf.GenerateReportDataPdfAsync(AssessmentRegisterRows(), SelectedDefinition.Name),
        OperationalReportType.WorkflowReadiness =>
            Pdf.GenerateReportDataPdfAsync(WorkflowRows.ToList(), SelectedDefinition.Name),
        OperationalReportType.AdvisorPerformance =>
            Pdf.GenerateReportDataPdfAsync(AdvisorRows.ToList(), SelectedDefinition.Name),
        OperationalReportType.BranchPerformance =>
            Pdf.GenerateReportDataPdfAsync(BranchRows.ToList(), SelectedDefinition.Name),
        _ => throw new InvalidOperationException("Unsupported operational report.")
    };

    private Task<byte[]> GenerateExcelAsync() => SelectedReport switch
    {
        OperationalReportType.AssessmentRegister =>
            Excel.GenerateDataReportExcelAsync(AssessmentRegisterRows(), WorksheetName()),
        OperationalReportType.WorkflowReadiness =>
            Excel.GenerateDataReportExcelAsync(WorkflowRows.ToList(), WorksheetName()),
        OperationalReportType.AdvisorPerformance =>
            Excel.GenerateDataReportExcelAsync(AdvisorRows.ToList(), WorksheetName()),
        OperationalReportType.BranchPerformance =>
            Excel.GenerateDataReportExcelAsync(BranchRows.ToList(), WorksheetName()),
        _ => throw new InvalidOperationException("Unsupported operational report.")
    };

    private List<AssessmentRegisterExportRow> AssessmentRegisterRows() =>
        FilteredRows.Select(row => new AssessmentRegisterExportRow
        {
            CaseNumber = row.CaseNumber,
            BusinessName = row.BusinessName,
            ClientName = row.ClientName,
            BranchName = row.BranchName,
            AdvisorName = row.AdvisorName,
            StatusName = row.StatusName,
            ReadinessPercentage = row.ProgressPercentage,
            StartDate = row.AssessmentStartDate,
            ExpectedFinishDate = row.AssessmentFinishDate,
            DaysOpen = row.DaysOpen,
            Active = row.Active
        }).ToList();

    private async Task DownloadAsync(byte[] bytes, string fileName, string contentType) =>
        await JS.InvokeVoidAsync(
            "viqReportOutput.downloadBase64",
            fileName,
            contentType,
            Convert.ToBase64String(bytes));

    private string BuildEmailBody(string message) => $"""
        <p>{WebUtility.HtmlEncode(message)}</p>
        <p><strong>Report:</strong> {WebUtility.HtmlEncode(SelectedDefinition.Name)}<br />
        <strong>Records:</strong> {FilteredRows.Count}<br />
        <strong>Generated:</strong> {DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC</p>
        <p>This report contains authorised ViabilityIQ tenant data. Please handle it securely.</p>
        """;

    private string BuildFileName(string extension) =>
        $"ViabilityIQ_{SelectedReport}_{DateTime.UtcNow:yyyyMMdd_HHmm}.{extension}";

    private string WorksheetName()
    {
        var name = SelectedDefinition.Name.Replace("&", "and", StringComparison.Ordinal);
        return name.Length <= 31 ? name : name[..31];
    }

    private static decimal Percentage(int value, int total) =>
        total == 0 ? 0 : Math.Round(value * 100m / total, 1);

    private async Task CancelCurrentLoadAsync()
    {
        if (_loadCancellation is null) return;
        await _loadCancellation.CancelAsync();
        _loadCancellation.Dispose();
        _loadCancellation = null;
    }

    public async ValueTask DisposeAsync()
    {
        await CancelCurrentLoadAsync();
        GC.SuppressFinalize(this);
    }

    protected sealed record ReportDefinition(
        OperationalReportType Type,
        string Name,
        string Description,
        string Icon);
}
