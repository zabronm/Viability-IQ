using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ViabilityIQ.Application.Interfaces
{
    public interface IPdfExportService
    {
        /// Generates a highly stylized, structured PDF document binary array from a master dataset list.       
        Task<byte[]> GenerateReportDataPdfAsync<T>(List<T> dataset, string documentTitle);
    }
}
