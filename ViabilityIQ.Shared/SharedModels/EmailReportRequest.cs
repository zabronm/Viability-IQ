using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ViabilityIQ.Shared.SharedModels
{
    public class EmailReportRequest
    {
        public string RecipientAddress { get; set; } = string.Empty;
        public string SubjectTitle { get; set; } = string.Empty;
        public string MessageBodyText { get; set; } = string.Empty;

        // Raw file binary data stream byte container for the attachment
        public byte[]? AttachmentBytes { get; set; }
        public string AttachmentName { get; set; } = "Report_Export.xlsx";
    }
}
