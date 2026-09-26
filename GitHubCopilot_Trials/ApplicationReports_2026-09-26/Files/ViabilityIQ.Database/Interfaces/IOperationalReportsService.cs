using ViabilityIQ.Shared.Reporting;

namespace ViabilityIQ.Application.Interfaces;

public interface IOperationalReportsService
{
    Task<OperationalReportFilterOptions> GetAssessmentFilterOptionsAsync(
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AssessmentOperationalReportRow>> GetAssessmentsAsync(
        AssessmentOperationalReportFilter filter,
        CancellationToken cancellationToken = default);
}
