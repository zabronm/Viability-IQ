using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViabilityIQ.Shared.DataModels.HomePageModels;

namespace ViabilityIQ.Application.Interfaces.HomePageInterfaces
{    
    /// Repository interface for activity log data access    
    public interface IActivityLogRepository
    {        
        Task<List<ActivityLogModel>> GetRecentActivitiesAsync(long userId, int count = 3, string filterType = "all");
        Task<List<ActivityLogModel>> GetActivitiesByDateRangeAsync(long userId, DateTime startDate, DateTime endDate);
    }
}