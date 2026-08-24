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
    
    /// Service for managing alert dismissals using Dapper for direct database access
    
    public class AlertDismissalService : IAlertDismissalService
    {
        private readonly ILogger<AlertDismissalService> _logger;
        private readonly IDbConnectionFactory _dbConnectionFactory;

        public AlertDismissalService(IDbConnectionFactory dbConnectionFactory, ILogger<AlertDismissalService> logger)
        {
            _dbConnectionFactory = dbConnectionFactory;
            _logger = logger;
        }

        /// <summary>
        /// Dismiss an alert and store the dismissal in the database
        /// Dismissed alerts are hidden for 7 days
        /// </summary>
        public async Task<bool> DismissAlertAsync(long userId, string alertId, string alertType)
        {
            try
            {
                _logger.LogInformation("Dismissing alert: {AlertId} of type: {AlertType} for userId: {UserId}",
                    alertId, alertType, userId);

                if (alertType == "Announcement")
                {
                    // Handle announcement dismissal
                    var query = @"
                        INSERT INTO UserAnnouncementDismissals (UserId, AnnouncementId, DismissalDate, DismissalExpiry)
                        VALUES (@UserId, @AnnouncementId, GETUTCDATE(), DATEADD(DAY, 7, GETUTCDATE()))
                    ";

                    using var connection = _dbConnectionFactory.CreateConnection();
                    await connection.ExecuteAsync(query, new
                    {
                        UserId = userId,
                        AnnouncementId = int.Parse(alertId)
                    });
                }
                else
                {
                    // Handle assessment/alert dismissal
                    var query = @"
                        INSERT INTO UserAlertDismissals (UserId, AlertId, AlertType, DismissalDate, DismissalExpiry)
                        VALUES (@UserId, @AlertId, @AlertType, GETUTCDATE(), DATEADD(DAY, 7, GETUTCDATE()))
                    ";

                    using var connection = _dbConnectionFactory.CreateConnection();
                    await connection.ExecuteAsync(query, new
                    {
                        UserId = userId,
                        AlertId = alertId,
                        AlertType = alertType
                    });
                }

                _logger.LogInformation("Alert dismissed successfully");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error dismissing alert for userId: {UserId}", userId);
                return false;
            }
        }

        /// <summary>
        /// Check if an alert has been dismissed by the user (and not expired)
        /// </summary>
        public async Task<bool> IsAlertDismissedAsync(long userId, string alertId)
        {
            try
            {
                _logger.LogInformation("Checking if alert is dismissed: {AlertId} for userId: {UserId}", alertId, userId);

                var query = @"
                    SELECT CASE
                        WHEN EXISTS (
                            SELECT 1 FROM UserAlertDismissals
                            WHERE UserId = @UserId
                            AND AlertId = @AlertId
                            AND DismissalExpiry > GETUTCDATE()
                        ) THEN 1
                        ELSE 0
                    END
                ";

                using var connection = _dbConnectionFactory.CreateConnection();
                var result = await connection.QueryFirstAsync<bool>(query, new
                {
                    UserId = userId,
                    AlertId = alertId
                });

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if alert is dismissed for userId: {UserId}", userId);
                return false;
            }
        }

        /// <summary>
        /// Restore a dismissed alert (remove dismissal from database)
        /// </summary>
        public async Task<bool> RestoreDismissedAlertAsync(long userId, string alertId)
        {
            try
            {
                _logger.LogInformation("Restoring dismissed alert: {AlertId} for userId: {UserId}", alertId, userId);

                var query = @"
                    DELETE FROM UserAlertDismissals
                    WHERE UserId = @UserId AND AlertId = @AlertId
                ";

                using var connection = _dbConnectionFactory.CreateConnection();
                await connection.ExecuteAsync(query, new
                {
                    UserId = userId,
                    AlertId = alertId
                });

                _logger.LogInformation("Alert restored successfully");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error restoring dismissed alert for userId: {UserId}", userId);
                return false;
            }
        }
    }
}
