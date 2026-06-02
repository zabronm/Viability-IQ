using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ViabilityIQ.Application.Interfaces
{
    public interface IExcelEPPlusExportService
    {
        Task<byte[]> GenerateDataReportExcelAsync<T>(List<T> dataset, string worksheetTitle);
    }
}
