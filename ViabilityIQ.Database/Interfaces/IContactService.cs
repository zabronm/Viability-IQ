
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViabilityIQ.Shared.DataModels;

namespace ViabilityIQ.Application.Interfaces
{

    public interface IContactService
    {        
        Task<bool> SubmitAsync(ContactRequest request);
    }


    public class ContactService: IContactService
    {
        private readonly ILogger<ContactService> _logger;
        public ContactService(ILogger<ContactService> logger) => _logger = logger;

        public async Task<bool> SubmitAsync(ContactRequest request)
        {
            // TODO: replace with real email send (SendGrid, Postmark)
            // or CRM push (HubSpot, Salesforce), or DB write.
            _logger.LogInformation(
                "New contact request from {Name} <{Email}> at {Company} — interest: {Interest}",
                request.Name, request.Email, request.Company, request.Interest);

            await Task.Delay(300); // simulate async I/O
            return true;
        }
    }
}
