using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ViabilityIQ.Application.Interfaces;
using ViabilityIQ.Shared.SharedModels;

namespace ViabilityIQ.Infrastructure.Reporting;

public sealed class EmailReportingService : IEmailReportingService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailReportingService> _logger;

    public EmailReportingService(
        IConfiguration configuration,
        ILogger<EmailReportingService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<EmailDeliveryResult> SendReportAsync(
        EmailReportRequest payload, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(payload);
        var reference = Guid.NewGuid().ToString("N");
        try
        {
            var recipient = new MailAddress(payload.RecipientAddress);
            var host = Required("EmailSettings:SmtpServer");
            var senderAddress = Required("EmailSettings:SenderAddress");
            var senderPassword = Required("EmailSettings:SenderPassword");
            if (!int.TryParse(Required("EmailSettings:Port"), out var port) || port is < 1 or > 65535)
                throw new EmailConfigurationException("EmailSettings:Port is invalid.");
            if (!bool.TryParse(Required("EmailSettings:EnableSsl"), out var enableSsl))
                throw new EmailConfigurationException("EmailSettings:EnableSsl must be true or false.");

            using var message = new MailMessage
            {
                From = new MailAddress(senderAddress, "ViabilityIQ"),
                Subject = payload.SubjectTitle,
                Body = payload.MessageBodyText,
                IsBodyHtml = true
            };
            message.To.Add(recipient);

            if (payload.AttachmentBytes is { Length: > 0 })
            {
                if (string.IsNullOrWhiteSpace(payload.AttachmentName)
                    || string.IsNullOrWhiteSpace(payload.AttachmentContentType))
                    throw new ArgumentException("Attachment filename and content type are required.");
                message.Attachments.Add(new Attachment(
                    new MemoryStream(payload.AttachmentBytes, writable: false),
                    payload.AttachmentName,
                    payload.AttachmentContentType));
            }

            using var client = new SmtpClient(host, port)
            {
                Credentials = new NetworkCredential(senderAddress, senderPassword),
                EnableSsl = enableSsl
            };
            await client.SendMailAsync(message).WaitAsync(cancellationToken);
            _logger.LogInformation("Report email sent with reference {Reference}", reference);
            return new(true, reference, null, null);
        }
        catch (EmailConfigurationException exception)
        {
            _logger.LogError(exception, "Report email configuration failure {Reference}", reference);
            return new(false, reference, "Configuration", exception.Message);
        }
        catch (FormatException exception)
        {
            _logger.LogWarning(exception, "Invalid report email address {Reference}", reference);
            return new(false, reference, "Validation", "The recipient email address is invalid.");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning("Report email cancelled {Reference}", reference);
            return new(false, reference, "Cancelled", "Email delivery was cancelled.");
        }
        catch (SmtpException exception)
        {
            _logger.LogError(exception, "SMTP report delivery failure {Reference}", reference);
            return new(false, reference, "Delivery", "The mail server rejected or could not deliver the message.");
        }
    }

    public async Task<bool> SendSystemReportWithAttachmentAsync(EmailReportRequest payload) =>
        (await SendReportAsync(payload)).Succeeded;

    private string Required(string key)
    {
        var value = _configuration[key];
        if (string.IsNullOrWhiteSpace(value))
            throw new EmailConfigurationException($"{key} is required.");
        return value;
    }

    private sealed class EmailConfigurationException(string message) : Exception(message);
}
