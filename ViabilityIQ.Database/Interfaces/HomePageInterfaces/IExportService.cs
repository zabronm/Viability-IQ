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
        /// <summary>
        /// Export data as Excel file
        /// </summary>
        /// <param name="exportType">Type of data to export (CompletionRate, StatusDistribution, TopPerformers, etc.)</param>
        /// <param name="userId">User ID (long/bigint) for filtering</param>
        Task<byte[]> ExportToExcelAsync(string exportType, long userId);

        /// <summary>
        /// Export data as PDF file
        /// </summary>
        /// <param name="exportType">Type of data to export</param>
        /// <param name="userId">User ID (long/bigint) for filtering</param>
        Task<byte[]> ExportToPdfAsync(string exportType, long userId);

        /// <summary>
        /// Get the appropriate file extension and content type for the format
        /// </summary>
        (string extension, string contentType) GetFileInfo(string format);
    }
}