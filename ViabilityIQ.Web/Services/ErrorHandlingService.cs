using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Data.Common;
using System.Text;
using ViabilityIQ.Shared.SharedModels;

namespace ViabilityIQ.Web.Services
{
    public interface IErrorHandlingService
    {
        Task<GlobalErrorDetails> HandleExceptionAsync(Exception ex, HttpContext context, ILogger logger);
        string GetErrorMessage(Exception ex);
        Task LogErrorToFileAsync(GlobalErrorDetails errorDetails, string logDirectory);
    }

    public class ErrorHandlingService : IErrorHandlingService
    {
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly ILogger<ErrorHandlingService> _logger;

        public ErrorHandlingService(IWebHostEnvironment webHostEnvironment, ILogger<ErrorHandlingService> logger)
        {
            _webHostEnvironment = webHostEnvironment;
            _logger = logger;
        }


        public async Task<GlobalErrorDetails> HandleExceptionAsync(Exception ex, HttpContext context, ILogger logger)
        {
            var errorDetails = new GlobalErrorDetails
            {
                ErrorCode = Guid.NewGuid().ToString(),
                StatusCode = GetStatusCode(ex),
                ErrorMessage = GetErrorMessage(ex),
                ErrorDetails = ex.Message,
                ExceptionType = ex.GetType().Name,
                Timestamp = DateTime.UtcNow,
                Path = context.Request.Path.ToString(),
                Method = context.Request.Method
            };

            if (_webHostEnvironment.IsDevelopment())
            {
                errorDetails.StackTrace = ex.StackTrace ?? string.Empty;
                errorDetails.ErrorDetails = BuildDetailedErrorMessage(ex);
            }

            logger.LogError(
                ex,
                "An unhandled exception occurred. ErrorCode: {ErrorCode}, StatusCode: {StatusCode}, ErrorMessage: {ErrorMessage}, Path: {Path}, Method: {Method}",
                errorDetails.ErrorCode,
                errorDetails.StatusCode,
                errorDetails.ErrorMessage,
                errorDetails.Path,
                errorDetails.Method
            );

            await LogErrorToFileAsync(errorDetails, Path.Combine(_webHostEnvironment.ContentRootPath, "Logs/Errors"));
            return errorDetails;
        }


        private int GetStatusCode(Exception ex)
        {
            // You can customize this method to return different status codes based on exception types
            return ex switch
            {
                TimeoutException => StatusCodes.Status408RequestTimeout,
                InvalidOperationException => StatusCodes.Status500InternalServerError,
                ArgumentNullException => StatusCodes.Status400BadRequest,
                ArgumentException => StatusCodes.Status400BadRequest,
                UnauthorizedAccessException => StatusCodes.Status401Unauthorized,
                KeyNotFoundException => StatusCodes.Status404NotFound,
                HttpRequestException => StatusCodes.Status502BadGateway,

                //Database specific errors
                SqlException => StatusCodes.Status500InternalServerError,
                DbUpdateException => StatusCodes.Status500InternalServerError,
                DBConcurrencyException => StatusCodes.Status409Conflict,
                DuplicateNameException => StatusCodes.Status409Conflict,
                DataException => StatusCodes.Status500InternalServerError,

                TaskCanceledException => StatusCodes.Status408RequestTimeout,
                OperationCanceledException => StatusCodes.Status408RequestTimeout,

                _ => StatusCodes.Status500InternalServerError,
            };
        }

        public string GetErrorMessage(Exception ex)
        {
            return ex switch
            {
                TimeoutException => "The request timed out. Please try again later.",
                InvalidOperationException => "An invalid operation occurred.",
                ArgumentNullException => "A required argument was null.",
                ArgumentException => "An argument provided was invalid.",
                UnauthorizedAccessException => "You do not have permission to perform this action.",
                KeyNotFoundException => "The requested resource was not found.",
                HttpRequestException => "An error occurred while making an HTTP request.",

                //Database specific errors
                SqlException => "A database error occurred.",
                DbUpdateException => "A database update error occurred.",
                DBConcurrencyException => "A concurrency conflict occurred while accessing the database.",
                DuplicateNameException => "A duplicate name error occurred in the database.",
                DataException => "A general data access error occurred.",
                TaskCanceledException => "The request was canceled due to a timeout.",
                OperationCanceledException => "The operation was canceled due to a timeout.",

                _ => "An unexpected error occurred. Please try again later."
            };
        }

        private string BuildDetailedErrorMessage(Exception ex)
        {
            var errorMessage = new StringBuilder();
            errorMessage.AppendLine($"Exception Type: {ex.GetType().FullName}");
            errorMessage.AppendLine($"Message: {ex.Message}");
            errorMessage.AppendLine($"Source: {ex.Source}");
            errorMessage.AppendLine($"StackTrace: {ex.StackTrace}");

            if (ex.InnerException != null)
            {
                errorMessage.AppendLine("\n ---- INNEER EXCEPTION ----:");
                errorMessage.AppendLine(BuildDetailedErrorMessage(ex.InnerException));
            }

            return errorMessage.ToString();
        }

        public async Task LogErrorToFileAsync(GlobalErrorDetails errorDetails, string logDirectory)
        {
            try
            {
                if (!Directory.Exists(logDirectory))
                {
                    Directory.CreateDirectory(logDirectory);
                }
                string fileName = $"error_{DateTime.UtcNow:yyyyMMdd_HHmmss}_{errorDetails.ErrorCode}.txt";
                string filePath = Path.Combine(logDirectory, fileName);

                var logContent = new StringBuilder();
                logContent.AppendLine("========================================");
                logContent.AppendLine("ERROR LOG");
                logContent.AppendLine("========================================");
                logContent.AppendLine($"Error ID: {errorDetails.ErrorCode}");
                logContent.AppendLine($"Timestamp: {errorDetails.Timestamp:O}");
                logContent.AppendLine($"Status Code: {errorDetails.StatusCode}");
                logContent.AppendLine($"Message: {errorDetails.ErrorMessage}");
                logContent.AppendLine($"Exception Type: {errorDetails.ExceptionType}");
                logContent.AppendLine($"Path: {errorDetails.Path}");
                logContent.AppendLine($"Method: {errorDetails.Method}");
                logContent.AppendLine();
                logContent.AppendLine("--- DETAILS ---");
                logContent.AppendLine(errorDetails.ErrorDetails);
                logContent.AppendLine();
                logContent.AppendLine("--- STACK TRACE ---");
                logContent.AppendLine(errorDetails.StackTrace);
                logContent.AppendLine("========================================");

                await File.WriteAllTextAsync(filePath, logContent.ToString());
            }
            catch (Exception fileEx)
            {
                _logger.LogError(fileEx, "Failed to write error details to file.");
            }
        }



    }
}
