using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ViabilityIQ.Application.Interfaces.HomePageInterfaces
{
    
    /// Service interface for exporting dashboard data    
    public interface IExportService
    {
       
        /// Export data as Excel file
       
        /// <param name="exportType">Type of data to export (CompletionRate, StatusDistribution, TopPerformers, etc.)</param>
        /// <param name="userId">User ID (long/bigint) for filtering</param>
        Task<byte[]> ExportToExcelAsync(string exportType, long userId);

       
        /// Export data as PDF file
       
        /// <param name="exportType">Type of data to export</param>
        /// <param name="userId">User ID (long/bigint) for filtering</param>
        Task<byte[]> ExportToPdfAsync(string exportType, long userId);

       
        /// Get the appropriate file extension and content type for the format
       
        (string extension, string contentType) GetFileInfo(string format);
    }
}