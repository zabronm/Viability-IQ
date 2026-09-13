using Dapper;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViabilityIQ.Application.Interfaces;
using ViabilityIQ.Infrastructure.DbFactory;
using ViabilityIQ.Shared.DataModels;

namespace ViabilityIQ.Infrastructure.Repositories
{
    internal class NotificationItemRepository : INotificationItemRepository
    {
        private readonly IDbConnectionFactory _dbConnectionFactory;

        public NotificationItemRepository(IDbConnectionFactory dbConnectionFactory)
        {
            _dbConnectionFactory = dbConnectionFactory;
        }

        public async Task<List<NotificationItem>> GetNotificationsAsync(long userId, string filter = "all")
        {
            try
            {
                using var _dbConnection = _dbConnectionFactory.CreateConnection();

                var sql = @"
                SELECT NotificationItemId, SenderId, RecipientId, Title, Message, CreatedDate, IsRead, IsTask, TaskStatus, DueDate, AssessmentNumber
                FROM tblNotificationItem
                WHERE RecipientId = @UserId
                  AND (@Filter = 'all' 
                       OR (@Filter = 'tasks' AND IsTask = 1) 
                       OR (@Filter = 'messages' AND IsTask = 0))
                ORDER BY CreatedDate DESC;";

                var results = await _dbConnection.QueryAsync<NotificationItem>(sql, new { UserId = userId, Filter = filter });
                return results.AsList();
            }
            catch (Exception)
            {
                throw;
            }           
        }


        public async Task<int> GetUnreadCountAsync(long userId)
        {
            using var _dbConnection = _dbConnectionFactory.CreateConnection();
            var sql = "SELECT COUNT(1) FROM tblNotificationItem WHERE RecipientId = @UserId AND IsRead = 0;";
            return await _dbConnection.ExecuteScalarAsync<int>(sql, new { UserId = userId });
        }


        public async Task<int> InsertNotificationAsync(NotificationItem item)
        {
            using var _dbConnection = _dbConnectionFactory.CreateConnection();
            var sql = @"
                INSERT INTO tblNotificationItem (SenderId, RecipientId, Title, Message, CreatedDate, IsRead, IsTask, TaskStatus, DueDate, AssessmentNumber, Active, CreatedBy)
                VALUES (@SenderId, @RecipientId, @Title, @Message, SYSUTCDATETIME(), 0, @IsTask, @TaskStatus, @DueDate, @AssessmentNumber, @Active, @CreatedBy);
                SELECT CAST(SCOPE_IDENTITY() as INT);";

            return await _dbConnection.ExecuteScalarAsync<int>(sql, item);
        }


        public async Task<bool> MarkAsReadAsync(int notificationId)
        {
            using var _dbConnection = _dbConnectionFactory.CreateConnection();
            var sql = "UPDATE tblNotificationItem SET IsRead = 1 WHERE NotificationItemId = @Id;";
            var rowsAffected = await _dbConnection.ExecuteAsync(sql, new { Id = notificationId });
            return rowsAffected > 0;
        }


        public async Task<bool> UpdateTaskStatusAsync(int notificationId, int taskStatus)
        {
            using var _dbConnection = _dbConnectionFactory.CreateConnection();
            var sql = "UPDATE tblNotificationItem SET TaskStatus = @TaskStatus WHERE NotificationItemId = @Id;";
            var rowsAffected = await _dbConnection.ExecuteAsync(sql, new { Id = notificationId, TaskStatus = taskStatus });
            return rowsAffected > 0;
        }
    }
}