namespace ViabilityIQ.Shared.DataModels.HomePageModels;

public sealed class ActivityLogModel
{
    public long Id { get; set; }
    public long? UserId { get; set; }
    public string ActorName { get; set; } = string.Empty;
    public string ActivityAction { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public long? EntityId { get; set; }
    public string EntityName { get; set; } = string.Empty;
    public long? AssessmentId { get; set; }
    public string AssessmentName { get; set; } = string.Empty;
    public string Module { get; set; } = string.Empty;
    public string Page { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
    public string UserAgent { get; set; } = string.Empty;
    public string CorrelationId { get; set; } = string.Empty;
    public string MetadataJson { get; set; } = string.Empty;
    public string Remarks { get; set; } = string.Empty;
    public DateTime CreatedDate { get; set; }
    public string? NavigationUrl { get; set; }
}
