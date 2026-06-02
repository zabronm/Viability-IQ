using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;
using ViabilityIQ.Application.Interfaces;
using ViabilityIQ.Shared.SharedModels;

namespace ViabilityIQ.Infrastructure.Reporting
{
    public class EmailReportingService: IEmailReportingService
    {
        private readonly IConfiguration _configuration;

        public EmailReportingService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task<bool> SendSystemReportWithAttachmentAsync(EmailReportRequest payload)
        {
            try
            {
                // 1. Fetch mail server pipeline variables safely from AppSettings configurations
                string smtpHost = _configuration["EmailSettings:SmtpServer"] ?? "smtp.mailtrap.io";
                int smtpPort = int.Parse(_configuration["EmailSettings:Port"] ?? "587");
                string senderMail = _configuration["EmailSettings:SenderAddress"] ?? "noreply@viabilityiq.co.za";
                string senderPwd = _configuration["EmailSettings:SenderPassword"] ?? string.Empty;
                bool enableSsl = bool.Parse(_configuration["EmailSettings:EnableSsl"] ?? "true");

                // 2. Build the underlying MailMessage envelope structure
                using var mailMessage = new MailMessage
                {
                    From = new MailAddress(senderMail, "ViabilityIQ System Engine"),
                    Subject = payload.SubjectTitle,
                    Body = payload.MessageBodyText,
                    IsBodyHtml = true
                };

                mailMessage.To.Add(payload.RecipientAddress);

                // 3. Process binary attachment streams if they are present in the incoming payload context
                if (payload.AttachmentBytes != null && payload.AttachmentBytes.Length > 0)
                {
                    var memoryStream = new MemoryStream(payload.AttachmentBytes);
                    var attachment = new Attachment(memoryStream, payload.AttachmentName, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
                    mailMessage.Attachments.Add(attachment);
                }

                // 4. Instantiate the SMTP Transport Client and transmit the data bundle
                using var smtpClient = new SmtpClient(smtpHost, smtpPort)
                {
                    Credentials = new NetworkCredential(senderMail, senderPwd),
                    EnableSsl = enableSsl
                };

                await smtpClient.SendMailAsync(mailMessage);
                return true;
            }
            catch (Exception ex)
            {
                // Log the precise physical socket or routing anomaly to the diagnostic logs console
                Console.WriteLine($"[CRITICAL] Mailing Subsystem Failure: {ex.Message}");
                throw;
            }
        }
    }
}