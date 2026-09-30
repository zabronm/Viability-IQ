using ViabilityIQ.Shared.Reporting;

namespace ViabilityIQ.Application.Interfaces;

public interface IAssessmentReportService
{
    Task<ReportDocument> BuildAsync(
        long assessmentId, ReportType reportType, int startMonth = 1, int endMonth = 12,
        CancellationToken cancellationToken = default);
}

public interface IReportWorkbookWriter
{
    byte[] Write(ReportDocument document);
}

public interface IReportActivityRepository
{
    Task<ReportActivityWriteResult> RecordAsync(
        ReportActivity activity, CancellationToken cancellationToken = default);
}

