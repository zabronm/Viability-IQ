using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViabilityIQ.Shared.SharedModels;

namespace ViabilityIQ.Application.Interfaces
{
    public interface IEmailReportingService
    {
        /// Dispatches a formatted structural text notification accompanied by optional binary document attachments.
         Task<bool> SendSystemReportWithAttachmentAsync(EmailReportRequest payload);
    }
}
