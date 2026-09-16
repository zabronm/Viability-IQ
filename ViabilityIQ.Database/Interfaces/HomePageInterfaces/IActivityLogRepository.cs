using ViabilityIQ.Shared.DataModels.HomePageModels;

namespace ViabilityIQ.Application.Interfaces.HomePageInterfaces;

public interface IActivityLogRepository
{
    Task<List<ActivityLogModel>> GetRecentActivitiesAsync(
        long userId,
        int count = 3,
        string filterType = "all");

    Task<List<ActivityLogModel>> GetActivitiesByDateRangeAsync(
        long userId,
        DateTime startDate,
        DateTime endDate);

    Task<ActivityLogPageResult> GetActivityPageAsync(ActivityLogQueryModel query);
}
