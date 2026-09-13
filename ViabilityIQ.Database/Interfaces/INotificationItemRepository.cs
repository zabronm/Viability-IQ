using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViabilityIQ.Shared.DataModels;


namespace ViabilityIQ.Application.Interfaces
{
    public interface INotificationItemRepository
    {
        Task<List<NotificationItem>> GetNotificationsAsync(long userId, string filter = "all");
        Task<int> GetUnreadCountAsync(long userId);
        Task<int> InsertNotificationAsync(NotificationItem notification);
        Task<bool> MarkAsReadAsync(int notificationId);
        Task<bool> UpdateTaskStatusAsync(int notificationId, int taskStatus);
    }
}
