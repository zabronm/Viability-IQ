using Dapper;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViabilityIQ.Application.Interfaces.HomePageInterfaces;
using ViabilityIQ.Infrastructure.DbFactory;

namespace ViabilityIQ.Infrastructure.Repositories.HomePageRepositories
{
    /// <summary>
    /// Service for exporting dashboard data to Excel and PDF formats using Dapper
    /// </summary>
    public class ExportService : IExportService
    {
        private readonly ILogger<ExportService> _logger;
        private readonly IDbConnectionFactory _dbConnectionFactory;

        public ExportService(IDbConnectionFactory dbConnectionFactory, ILogger<ExportService> logger)
        {
            _dbConnectionFactory = dbConnectionFactory;
            _logger = logger;
        }

        /// <summary>
        /// Export data as Excel file
        /// For tabular data: returns raw data with formulas
        /// For charts: returns data with embedded charts
        /// </summary>
        public async Task<byte[]> ExportToExcelAsync(string exportType, long userId)
        {
            try
            {
                _logger.LogInformation("Exporting {ExportType} to Excel for userId: {UserId}", exportType, userId);

                byte[] excelFile = exportType switch
                {
                    "CompletionRate" => await ExportCompletionRateAsync(userId, "Excel"),
                    "AvgCompletionTime" => await ExportAvgCompletionTimeAsync(userId, "Excel"),
                    "StatusDistribution" => await ExportStatusDistributionAsync(userId, "Excel"),
                    "TopPerformers" => await ExportTopPerformersAsync(userId, "Excel"),
                    _ => Array.Empty<byte>()
                };

                _logger.LogInformation("Excel export completed successfully ({Bytes} bytes)", excelFile.Length);
                return excelFile;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting to Excel for userId: {UserId}", userId);
                return Array.Empty<byte>();
            }
        }

        /// <summary>
        /// Export data as PDF file
        /// For charts: returns formatted chart as PDF
        /// For tabular data: returns formatted report as PDF
        /// </summary>
        public async Task<byte[]> ExportToPdfAsync(string exportType, long userId)
        {
            try
            {
                _logger.LogInformation("Exporting {ExportType} to PDF for userId: {UserId}", exportType, userId);

                byte[] pdfFile = exportType switch
                {
                    "CompletionRate" => await ExportCompletionRateAsync(userId, "PDF"),
                    "AvgCompletionTime" => await ExportAvgCompletionTimeAsync(userId, "PDF"),
                    "StatusDistribution" => await ExportStatusDistributionAsync(userId, "PDF"),
                    "TopPerformers" => await ExportTopPerformersAsync(userId, "PDF"),
                    _ => Array.Empty<byte>()
                };

                _logger.LogInformation("PDF export completed successfully ({Bytes} bytes)", pdfFile.Length);
                return pdfFile;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting to PDF for userId: {UserId}", userId);
                return Array.Empty<byte>();
            }
        }

        /// <summary>
        /// Get the appropriate file extension and content type for the format
        /// </summary>
        public (string extension, string contentType) GetFileInfo(string format)
        {
            return format?.ToLower() switch
            {
                "excel" => (".xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"),
                "pdf" => (".pdf", "application/pdf"),
                _ => (".txt", "text/plain")
            };
        }

        #region Private Export Methods

        /// <summary>
        /// Export completion rate metric
        /// </summary>
        private async Task<byte[]> ExportCompletionRateAsync(long userId, string format)
        {
            try
            {
                _logger.LogInformation("Exporting completion rate as {Format} for userId: {UserId}", format, userId);

                var query = @"
                    DECLARE @CurrentMonth INT = MONTH(GETUTCDATE());
                    DECLARE @CurrentYear INT = YEAR(GETUTCDATE());
                    DECLARE @PreviousMonth INT = MONTH(DATEADD(MONTH, -1, GETUTCDATE()));
                    DECLARE @PreviousYear INT = YEAR(DATEADD(MONTH, -1, GETUTCDATE()));

                    SELECT
                        'Completion Rate' AS MetricName,
                        CAST(
                            (SELECT COUNT(*) FROM Assessments 
                            WHERE AssignedToUserId = @UserId 
                            AND Status = 'Completed'
                            AND MONTH(CompletedDate) = @CurrentMonth 
                            AND YEAR(CompletedDate) = @CurrentYear) 
                            * 100.0 / 
                            (SELECT COUNT(*) FROM Assessments 
                            WHERE AssignedToUserId = @UserId 
                            AND MONTH(CreatedDate) = @CurrentMonth 
                            AND YEAR(CreatedDate) = @CurrentYear) AS INT
                        ) AS CurrentValue,
                        'Percent' AS Unit,
                        FORMAT(GETUTCDATE(), 'yyyy-MM-dd HH:mm:ss') AS ExportDate
                ";

                using var connection = _dbConnectionFactory.CreateConnection();
                var data = await connection.QueryAsync(query, new { UserId = userId });

                // TODO: Implement actual Excel/PDF generation using libraries like:
                // - ClosedXML for Excel
                // - iTextSharp or PdfSharp for PDF
                // This is a placeholder returning empty array
                return Array.Empty<byte>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting completion rate for userId: {UserId}", userId);
                return Array.Empty<byte>();
            }
        }

        /// <summary>
        /// Export average completion time metric
        /// </summary>
        private async Task<byte[]> ExportAvgCompletionTimeAsync(long userId, string format)
        {
            try
            {
                _logger.LogInformation("Exporting average completion time as {Format} for userId: {UserId}", format, userId);

                var query = @"
                    DECLARE @CurrentMonth INT = MONTH(GETUTCDATE());
                    DECLARE @CurrentYear INT = YEAR(GETUTCDATE());
                    DECLARE @PreviousMonth INT = MONTH(DATEADD(MONTH, -1, GETUTCDATE()));
                    DECLARE @PreviousYear INT = YEAR(DATEADD(MONTH, -1, GETUTCDATE()));

                    SELECT
                        'Avg Completion Time (Days)' AS MetricName,
                        COALESCE(
                            CAST(AVG(DATEDIFF(DAY, CreatedDate, CompletedDate)) AS INT),
                            0
                        ) AS CurrentValue,
                        'Days' AS Unit,
                        FORMAT(GETUTCDATE(), 'yyyy-MM-dd HH:mm:ss') AS ExportDate
                    FROM Assessments
                    WHERE AssignedToUserId = @UserId 
                    AND Status = 'Completed'
                    AND MONTH(CompletedDate) = @CurrentMonth 
                    AND YEAR(CompletedDate) = @CurrentYear
                ";

                using var connection = _dbConnectionFactory.CreateConnection();
                var data = await connection.QueryAsync(query, new { UserId = userId });

                // TODO: Implement actual Excel/PDF generation
                return Array.Empty<byte>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting average completion time for userId: {UserId}", userId);
                return Array.Empty<byte>();
            }
        }

        /// <summary>
        /// Export assessment status distribution data
        /// </summary>
        private async Task<byte[]> ExportStatusDistributionAsync(long userId, string format)
        {
            try
            {
                _logger.LogInformation("Exporting status distribution as {Format} for userId: {UserId}", format, userId);

                var query = @"
                    SELECT
                        a.Status,
                        COUNT(*) AS Count,
                        CAST(COUNT(*) * 100.0 / (SELECT COUNT(*) FROM Assessments WHERE AssignedToUserId = @UserId) AS INT) AS Percentage,
                        FORMAT(GETUTCDATE(), 'yyyy-MM-dd HH:mm:ss') AS ExportDate
                    FROM Assessments a
                    WHERE a.AssignedToUserId = @UserId
                    GROUP BY a.Status
                    ORDER BY COUNT(*) DESC
                ";

                using var connection = _dbConnectionFactory.CreateConnection();
                var data = await connection.QueryAsync(query, new { UserId = userId });

                // TODO: Implement actual Excel/PDF generation with chart
                return Array.Empty<byte>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting status distribution for userId: {UserId}", userId);
                return Array.Empty<byte>();
            }
        }

        /// <summary>
        /// Export top performers leaderboard
        /// </summary>
        private async Task<byte[]> ExportTopPerformersAsync(long userId, string format)
        {
            try
            {
                _logger.LogInformation("Exporting top performers as {Format} for userId: {UserId}", format, userId);

                var query = @"
                    DECLARE @CurrentMonth INT = MONTH(GETUTCDATE());
                    DECLARE @CurrentYear INT = YEAR(GETUTCDATE());

                    SELECT
                        ROW_NUMBER() OVER (ORDER BY COUNT(a.AssessmentId) DESC) AS Rank,
                        CONCAT(u.FirstName, ' ', u.LastName) AS PerformerName,
                        COUNT(a.AssessmentId) AS CompletedCount,
                        FORMAT(GETUTCDATE(), 'yyyy-MM-dd HH:mm:ss') AS ExportDate
                    FROM AspNetUsers u
                    INNER JOIN Assessments a ON u.Id = a.AssignedToUserId
                    WHERE a.Status = 'Completed'
                    AND MONTH(a.CompletedDate) = @CurrentMonth
                    AND YEAR(a.CompletedDate) = @CurrentYear
                    GROUP BY u.Id, u.FirstName, u.LastName
                    ORDER BY COUNT(a.AssessmentId) DESC
                ";

                using var connection = _dbConnectionFactory.CreateConnection();
                var data = await connection.QueryAsync(query);

                // TODO: Implement actual Excel/PDF generation
                return Array.Empty<byte>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting top performers for userId: {UserId}", userId);
                return Array.Empty<byte>();
            }
        }

        #endregion
    }
}
