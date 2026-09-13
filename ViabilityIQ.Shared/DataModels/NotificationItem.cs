using Dapper.Contrib.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViabilityIQ.Shared.DataModelsInterfaces;

namespace ViabilityIQ.Shared.DataModels
{
    [Table("tblNotificationItem")]
    public class NotificationItem : IEntity, ISortableEntity, IAuditableEntity
    {
        [Key] public int NotificationItemId { get; set; }
        public long SenderId { get; set; }
        public long RecipientId { get; set; }

        // Assessment/Case Tracking Link
        public string? AssessmentId { get; set; }
        public string? AssessmentNumber { get; set; }

        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;       
        public bool IsRead { get; set; }

        // Task Tracking Properties
        public bool IsTask { get; set; }
        public TaskStatusType TaskStatus { get; set; } = TaskStatusType.Pending;
        public DateTime? DueDate { get; set; }
               
        public string? Remarks { get; set; }
        public bool Active { get; set; } = true;
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public long CreatedBy { get; set; }
        public DateTime ModifiedDate { get; set; } = DateTime.UtcNow;
        public long ModifiedBy { get; set; }

        long IEntity.Id => NotificationItemId;
        string ISortableEntity.DisplayName => Title;
    }

    public enum TaskStatusType
    {
        Pending = 0,
        InProgress = 1,
        Completed = 2
    }
}
