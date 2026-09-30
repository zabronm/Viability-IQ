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
using ViabilityIQ.Shared.DataModels.HomePageModels;


namespace ViabilityIQ.Infrastructure.Repositories.HomePageRepositories
{
    
    /// Repository for system announcement data access using Dapper    
    public class AnnouncementRepository : IAnnouncementRepository
    {
        private readonly IDbConnectionFactory _dbConnectionFactory;
        private readonly ILogger<AnnouncementRepository> _logger;

        public AnnouncementRepository(IDbConnectionFactory dbConnectionFactory, ILogger<AnnouncementRepository> logger)
        {
            _dbConnectionFactory = dbConnectionFactory;
            _logger = logger;
        }

        
        /// Get all active system announcements        
        public async Task<List<SystemAnnouncementModel>> GetActiveAnnouncementsAsync()
        {
            try
            {
                _logger.LogInformation("Fetching active announcements");

                var query = @"
                    SELECT
                        a.AnnouncementId AS Id,
                        a.Title,
                        a.Message,
                        a.AnnouncementType,
                        a.CreatedDate,
                        a.ExpiryDate,
                        a.IsActive
                    FROM SystemAnnouncements a
                    WHERE a.IsActive = 1
                    AND (a.ExpiryDate IS NULL OR a.ExpiryDate > GETUTCDATE())
                    ORDER BY a.CreatedDate DESC
                ";

                using var connection = _dbConnectionFactory.CreateConnection();
                var announcements = (await connection.QueryAsync<SystemAnnouncementModel>(query)).ToList();

                _logger.LogInformation("Retrieved {Count} active announcements", announcements.Count);
                return announcements;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching active announcements");
                return new List<SystemAnnouncementModel>();
            }
        }

        
        /// Get announcements that the user hasn't dismissed
        
        public async Task<List<SystemAnnouncementModel>> GetNonDismissedAnnouncementsAsync(long userId)
        {
            try
            {
                _logger.LogInformation("Fetching non-dismissed announcements for user: {UserId}", userId);

                var query = @"
                    SELECT
                        a.AnnouncementId AS Id,
                        a.Title,
                        a.Message,
                        a.AnnouncementType,
                        a.CreatedDate,
                        a.ExpiryDate,
                        a.IsActive
                    FROM SystemAnnouncements a
                    LEFT JOIN UserAnnouncementDismissals ud ON a.AnnouncementId = ud.AnnouncementId 
                        AND ud.UserId = @UserId
                        AND ud.DismissalExpiry > GETUTCDATE()
                    WHERE a.IsActive = 1
                    AND (a.ExpiryDate IS NULL OR a.ExpiryDate > GETUTCDATE())
                    AND ud.DismissalId IS NULL
                    ORDER BY a.CreatedDate DESC
                ";

                using var connection = _dbConnectionFactory.CreateConnection();
                var announcements = (await connection.QueryAsync<SystemAnnouncementModel>(
                    query,
                    new { UserId = userId }
                )).ToList();

                _logger.LogInformation("Retrieved {Count} non-dismissed announcements", announcements.Count);
                return announcements;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching non-dismissed announcements");
                return new List<SystemAnnouncementModel>();
            }
        }

        
        /// Check if an announcement has been dismissed by the user (and dismissal not expired)        
        public async Task<bool> IsAnnouncementDismissedAsync(long userId, int announcementId)
        {
            try
            {
                _logger.LogInformation("Checking if announcement {AnnouncementId} is dismissed for user: {UserId}",
                    announcementId, userId);

                var query = @"
                    SELECT CASE
                        WHEN EXISTS (
                            SELECT 1 FROM UserAnnouncementDismissals
                            WHERE UserId = @UserId
                            AND AnnouncementId = @AnnouncementId
                            AND DismissalExpiry > GETUTCDATE()
                        ) THEN 1
                        ELSE 0
                    END
                ";

                using var connection = _dbConnectionFactory.CreateConnection();
                var isDismissed = await connection.QueryFirstAsync<bool>(
                    query,
                    new { UserId = userId, AnnouncementId = announcementId }
                );

                return isDismissed;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if announcement is dismissed");
                return false;
            }
        }
    }
}