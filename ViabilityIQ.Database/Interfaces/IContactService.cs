using ViabilityIQ.Shared.DataModels;

namespace ViabilityIQ.Application.Interfaces
{

    public interface IContactService
    {
        Task<ContactSubmissionResult> SubmitAsync(
            ContactRequest request,
            CancellationToken cancellationToken = default);
    }

    public sealed record ContactSubmissionResult(
        bool Accepted,
        bool EmailDelivered,
        long LeadSubmissionId,
        string Reference,
        string UserMessage);
}
