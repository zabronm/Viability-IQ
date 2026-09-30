namespace ViabilityIQ.Shared.DataModels.HomePageModels;

public sealed class ActivityLogQueryModel
{
    public long UserId { get; set; }
    public string? SearchText { get; set; }
    public string? ActivityAction { get; set; }
    public string? EntityType { get; set; }
    public DateTime? FromUtc { get; set; }
    public DateTime? ToUtcExclusive { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public sealed class ActivityLogPageResult
{
    public List<ActivityLogModel> Items { get; set; } = new();
    public int FilteredCount { get; set; }
    public int TotalCount { get; set; }
    public int TodayCount { get; set; }
    public int LastSevenDaysCount { get; set; }
    public int ActorCount { get; set; }
}
