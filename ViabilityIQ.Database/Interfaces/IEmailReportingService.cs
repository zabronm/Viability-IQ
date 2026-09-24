using ViabilityIQ.Shared.SharedModels;

namespace ViabilityIQ.Application.Interfaces;

public interface IEmailReportingService
{
    Task<EmailDeliveryResult> SendReportAsync(
        EmailReportRequest payload, CancellationToken cancellationToken = default);

    Task<bool> SendSystemReportWithAttachmentAsync(EmailReportRequest payload);
}
