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
    
    /// Repository for activity log data access using Dapper
    
    public class ActivityLogRepository : IActivityLogRepository
    {
        private readonly IDbConnectionFactory _dbConnectionFactory;
        private readonly ILogger<ActivityLogRepository> _logger;

        public ActivityLogRepository(IDbConnectionFactory dbConnectionFactory, ILogger<ActivityLogRepository> logger)
        {
            _dbConnectionFactory = dbConnectionFactory;
            _logger = logger;
        }

        
        /// Get recent activities for a specific user (top N records)
        /// Ordered by most recent first
        
        public async Task<List<ActivityLogModel>> GetRecentActivitiesAsync(long userId, int count = 3, string filterType = "all")
        {
            try
            {
                _logger.LogInformation("Fetching {Count} recent activities for user: {UserId}, filter: {FilterType}",
                    count, userId, filterType);

                var query = @"
                    SELECT TOP (@Count)
                        al.ActivityLogId AS Id,
                        CONCAT(u.FirstName, ' ', u.LastName) AS ActorName,
                        al.ActivityType,
                        al.ObjectName,
                        al.CreatedDate,
                        al.NavigationUrl
                    FROM ActivityLogs al
                    INNER JOIN AspNetUsers u ON al.UserId = u.Id
                    WHERE al.UserId = @UserId
                ";

                // Apply date range filter
                query = filterType switch
                {
                    "24h" => query + "AND al.CreatedDate >= DATEADD(HOUR, -24, GETUTCDATE())",
                    "7d" => query + "AND al.CreatedDate >= DATEADD(DAY, -7, GETUTCDATE())",
                    "30d" => query + "AND al.CreatedDate >= DATEADD(DAY, -30, GETUTCDATE())",
                    _ => query // "all" - no date filter
                };

                query += " ORDER BY al.CreatedDate DESC";

                using var connection = _dbConnectionFactory.CreateConnection();
                var activities = (await connection.QueryAsync<ActivityLogModel>(
                    query,
                    new { UserId = userId, Count = count }
                )).ToList();

                _logger.LogInformation("Retrieved {Count} activities", activities.Count);
                return activities;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching recent activities");
                return new List<ActivityLogModel>();
            }
        }

        
        /// Get activities within a specific date range
        
        public async Task<List<ActivityLogModel>> GetActivitiesByDateRangeAsync(long userId, DateTime startDate, DateTime endDate)
        {
            try
            {
                _logger.LogInformation("Fetching activities for user: {UserId} between {StartDate} and {EndDate}",
                    userId, startDate, endDate);

                var query = @"
                    SELECT
                        al.ActivityLogId AS Id,
                        CONCAT(u.FirstName, ' ', u.LastName) AS ActorName,
                        al.ActivityType,
                        al.ObjectName,
                        al.CreatedDate,
                        al.NavigationUrl
                    FROM ActivityLogs al
                    INNER JOIN AspNetUsers u ON al.UserId = u.Id
                    WHERE al.UserId = @UserId
                    AND al.CreatedDate BETWEEN @StartDate AND @EndDate
                    ORDER BY al.CreatedDate DESC
                ";

                using var connection = _dbConnectionFactory.CreateConnection();
                var activities = (await connection.QueryAsync<ActivityLogModel>(
                    query,
                    new { UserId = userId, StartDate = startDate, EndDate = endDate }
                )).ToList();

                _logger.LogInformation("Retrieved {Count} activities in date range", activities.Count);
                return activities;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching activities by date range");
                return new List<ActivityLogModel>();
            }
        }
    }
}