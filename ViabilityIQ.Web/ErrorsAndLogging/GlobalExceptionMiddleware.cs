using ViabilityIQ.Shared.SharedModels;
using ViabilityIQ.Web.ErrorsAndLogging;
using ViabilityIQ.Web.Services;

namespace ViabilityIQ.Web.ErrorsAndLogging
{
    public class GlobalExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionMiddleware> _logger;

        public GlobalExceptionMiddleware(
            RequestDelegate next,
            ILogger<GlobalExceptionMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(
            HttpContext context,
            IWebHostEnvironment webHostEnvironment,
            IErrorHandlingService errorHandlingService,
            ToastService toastService)  // ← INJECT ToastService
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(
                    context,
                    ex,
                    webHostEnvironment,
                    errorHandlingService,
                    toastService);  // ← PASS IT
            }
        }

        private async Task HandleExceptionAsync(
            HttpContext context,
            Exception exception,
            IWebHostEnvironment webHostEnvironment,
            IErrorHandlingService errorHandlingService,
            ToastService toastService)  // ← RECEIVE IT
        {
            _logger.LogError(exception, "Unhandled exception occurred");

            var errorDetails = await errorHandlingService.HandleExceptionAsync(exception, context, _logger);
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = errorDetails.StatusCode;

            // ============================================================================
            // SHOW TOAST NOTIFICATION TO USER
            // ============================================================================
            NotifyUserOfError(exception, errorDetails, toastService);

            // ============================================================================
            // BUILD JSON RESPONSE
            // ============================================================================
            var response = new
            {
                errorDetails.ErrorCode,
                errorDetails.StatusCode,
                errorDetails.ErrorMessage,
                errorDetails.Timestamp,
                Details = webHostEnvironment.IsDevelopment() ? errorDetails.ErrorDetails : null,
                StackTrace = webHostEnvironment.IsDevelopment() ? errorDetails.StackTrace : null
            };

            if (context.Request.Path.StartsWithSegments("/register") ||
                context.Request.Path.StartsWithSegments("/login") ||
                context.Request.Path.StartsWithSegments("/dashboard"))
            {
                var encodedMessage = System.Net.WebUtility.UrlEncode(errorDetails.ErrorMessage);
                var encodedDetails = System.Net.WebUtility.UrlEncode(
                    webHostEnvironment.IsDevelopment() ? errorDetails.ErrorDetails : string.Empty);
                var encodedErrorId = System.Net.WebUtility.UrlEncode(errorDetails.ErrorCode);

                context.Response.Redirect($"/error?errorMessage={encodedMessage}&errorDetails={encodedDetails}&errorId={encodedErrorId}");
                return;
            }

            await context.Response.WriteAsJsonAsync(response);
        }

        
        /// Determines the appropriate toast message based on exception type
        /// and shows it to the user
        
        private void NotifyUserOfError(Exception exception, GlobalErrorDetails errorDetails, ToastService toastService)
        {
            try
            {
                // Database Connection Errors
                if (exception is TimeoutException)
                {
                    toastService.ShowError(
                        "The request took too long to complete. The database or service may be unavailable. Please try again.",
                        "Request Timeout");
                    return;
                }

                if (exception is InvalidOperationException invalidOpEx &&
                    invalidOpEx.Message.Contains("database", StringComparison.OrdinalIgnoreCase))
                {
                    toastService.ShowError(
                        "Database connection failed. Please ensure your database server is running and accessible.",
                        "Database Connection Error");
                    return;
                }

                if (exception is Microsoft.Data.SqlClient.SqlException sqlEx)
                {
                    toastService.ShowError(
                        $"Database error: {sqlEx.Message}. Please check your database connection.",
                        "Database Error");
                    return;
                }

                // Network Errors
                if (exception is HttpRequestException)
                {
                    toastService.ShowError(
                        "Network error occurred. Please check your internet connection and try again.",
                        "Network Error");
                    return;
                }

                // Authentication/Authorization Errors
                if (exception is UnauthorizedAccessException)
                {
                    toastService.ShowError(
                        "You do not have permission to access this resource.",
                        "Access Denied");
                    return;
                }

                // Null Reference Errors
                if (exception is NullReferenceException)
                {
                    toastService.ShowError(
                        "An unexpected error occurred. Please try refreshing the page.",
                        "Application Error");
                    return;
                }

                // Generic Server Errors
                if (errorDetails.StatusCode >= 500)
                {
                    toastService.ShowError(
                        $"Server error occurred. Error ID: {errorDetails.ErrorCode}. Our team has been notified.",
                        "Server Error");
                    return;
                }

                // Validation/Client Errors
                if (errorDetails.StatusCode >= 400 && errorDetails.StatusCode < 500)
                {
                    toastService.ShowWarning(
                        errorDetails.ErrorMessage,
                        "Invalid Request");
                    return;
                }

                // Fallback for unknown errors
                toastService.ShowError(
                    "An unexpected error occurred. Please try again.",
                    "Error");
            }
            catch (Exception ex)
            {
                // If showing toast fails, just log it
                _logger.LogError(ex, "Failed to show error toast to user");
            }
        }
    }
}