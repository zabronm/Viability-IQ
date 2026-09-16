using Dapper.Contrib.Extensions;
using ViabilityIQ.Shared.DataModelsInterfaces;

namespace ViabilityIQ.Shared.DataModels;

[Table("tblActivityLog")]
public class ActivityLog : IEntity, IAuditableEntity, ISortableEntity
{
    [Key] public long ActivityLogId { get; set; }

    public long? UserId { get; set; }
    public string? ActorName { get; set; }
    public string ActivityAction { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public long? EntityId { get; set; }
    public string? EntityName { get; set; }
    public long? AssessmentId { get; set; }
    public string? AssessmentName { get; set; }
    public string? Module { get; set; }
    public string? Page { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public string? CorrelationId { get; set; }
    public string? MetadataJson { get; set; }
    public string? Remarks { get; set; }
    public bool Active { get; set; } = true;
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public long CreatedBy { get; set; }
    public DateTime ModifiedDate { get; set; } = DateTime.UtcNow;
    public long ModifiedBy { get; set; }

    long IEntity.Id => ActivityLogId;
    string ISortableEntity.DisplayName => Remarks ?? EntityName ?? ActivityAction;
}
