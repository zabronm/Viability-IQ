using System.Data;
using ViabilityIQ.Shared.SharedModels;

namespace ViabilityIQ.Application.Interfaces;

public interface IActivityLogWriter
{
    Task RecordAsync(
        ActivityLogWriteRequest request,
        IDbConnection? connection = null,
        IDbTransaction? transaction = null,
        CancellationToken cancellationToken = default);
}

public sealed class ActivityLogWriteRequest
{
    public ActivityAction Action { get; init; }
    public string EntityType { get; init; } = string.Empty;
    public long? EntityId { get; init; }
    public string? EntityName { get; init; }
    public long? AssessmentId { get; init; }
    public string? AssessmentName { get; init; }
    public string? Module { get; init; }
    public string? Page { get; init; }
    public string? Remarks { get; init; }
    public object? Metadata { get; init; }
    public long? UserId { get; init; }
    public string? ActorName { get; init; }
}
