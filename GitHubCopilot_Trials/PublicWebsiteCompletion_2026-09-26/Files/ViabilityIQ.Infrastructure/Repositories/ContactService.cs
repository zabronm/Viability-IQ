using System.ComponentModel.DataAnnotations;
using System.Net;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ViabilityIQ.Application.Interfaces;
using ViabilityIQ.Shared.DataModels;
using ViabilityIQ.Shared.SharedModels;

namespace ViabilityIQ.Infrastructure.Repositories;

public sealed class ContactService(
    ILeadRepository leadRepository,
    IEmailReportingService emailReportingService,
    IConfiguration configuration,
    ILogger<ContactService> logger)
    : IContactService
{
    public async Task<ContactSubmissionResult> SubmitAsync(
        ContactRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        Validator.ValidateObject(
            request,
            new ValidationContext(request),
            validateAllProperties: true);

        if (!string.IsNullOrWhiteSpace(request.Website))
        {
            logger.LogWarning("Website contact honeypot rejected a submission.");
            return new(false, false, 0, string.Empty,
                "Your message could not be submitted.");
        }

        var lead = new LeadSubmission
        {
            FullName = request.Name.Trim(),
            Email = request.Email.Trim(),
            PhoneNumber = EmptyToNull(request.PhoneNumber),
            CompanyName = EmptyToNull(request.Company),
            Subject = request.Subject,
            Message = request.Message.Trim(),
            Status = "New",
            SubmittedAtUtc = DateTime.UtcNow
        };

        var leadSubmissionId = await leadRepository.InsertLeadAsync(
            lead,
            cancellationToken);
        var reference = $"VIQ-WEB-{leadSubmissionId:D6}";

        var recipient = configuration["Website:ContactRecipient"]
            ?? configuration["EmailSettings:SenderAddress"];
        if (string.IsNullOrWhiteSpace(recipient))
        {
            logger.LogError(
                "Lead {LeadSubmissionId} was stored but no website contact recipient is configured.",
                leadSubmissionId);
            return StoredWithoutEmail(leadSubmissionId, reference);
        }

        var delivery = await emailReportingService.SendReportAsync(
            new EmailReportRequest
            {
                RecipientAddress = recipient,
                SubjectTitle = $"Website enquiry [{request.Subject}] - {reference}",
                MessageBodyText = BuildEmailBody(lead, reference)
            },
            cancellationToken);

        if (!delivery.Succeeded)
        {
            logger.LogError(
                "Lead {LeadSubmissionId} was stored but email delivery failed. Category {Category}, reference {DeliveryReference}",
                leadSubmissionId,
                delivery.ErrorCategory,
                delivery.Reference);
            return StoredWithoutEmail(leadSubmissionId, reference);
        }

        await leadRepository.MarkEmailSentAsync(
            leadSubmissionId,
            DateTime.UtcNow,
            cancellationToken);
        logger.LogInformation(
            "Website lead {LeadSubmissionId} stored and notification sent. Reference {Reference}",
            leadSubmissionId,
            reference);

        return new(
            true,
            true,
            leadSubmissionId,
            reference,
            "Thank you. Your message has been received and our team will respond as soon as possible.");
    }

    private static ContactSubmissionResult StoredWithoutEmail(
        long leadSubmissionId,
        string reference) => new(
            true,
            false,
            leadSubmissionId,
            reference,
            "Your message was saved, but the email notification could not be delivered. Our team can still review your enquiry.");

    private static string BuildEmailBody(
        LeadSubmission lead,
        string reference) => $"""
            <h2>New ViabilityIQ website enquiry</h2>
            <table style="border-collapse:collapse">
              <tr><th style="text-align:left;padding:4px 12px 4px 0">Reference</th><td>{Encode(reference)}</td></tr>
              <tr><th style="text-align:left;padding:4px 12px 4px 0">Subject</th><td>{Encode(lead.Subject)}</td></tr>
              <tr><th style="text-align:left;padding:4px 12px 4px 0">Name</th><td>{Encode(lead.FullName)}</td></tr>
              <tr><th style="text-align:left;padding:4px 12px 4px 0">Email</th><td>{Encode(lead.Email)}</td></tr>
              <tr><th style="text-align:left;padding:4px 12px 4px 0">Phone</th><td>{Encode(lead.PhoneNumber)}</td></tr>
              <tr><th style="text-align:left;padding:4px 12px 4px 0">Company</th><td>{Encode(lead.CompanyName)}</td></tr>
              <tr><th style="text-align:left;padding:4px 12px 4px 0">Submitted UTC</th><td>{lead.SubmittedAtUtc:yyyy-MM-dd HH:mm:ss}</td></tr>
            </table>
            <h3>Message</h3>
            <p>{Encode(lead.Message).Replace("\r\n", "<br>").Replace("\n", "<br>")}</p>
            """;

    private static string Encode(string? value) =>
        WebUtility.HtmlEncode(value ?? "Not supplied");

    private static string? EmptyToNull(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
