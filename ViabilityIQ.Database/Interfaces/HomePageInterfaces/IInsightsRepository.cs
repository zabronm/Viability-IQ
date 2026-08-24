using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViabilityIQ.Shared.DataModels.HomePageModels;

namespace ViabilityIQ.Application.Interfaces.HomePageInterfaces
{    
    /// Repository interface for insights and analytics data access
    
    public interface IInsightsRepository
    {
        /// <summary>
        /// Get comprehensive insights data for a user
        /// </summary>
        Task<InsightsModel> GetInsightsAsync(long userId);

        /// <summary>
        /// Get insights for a specific branch (admin/manager only)
        /// </summary>
        Task<InsightsModel> GetBranchInsightsAsync(int branchId);

        /// <summary>
        /// Get top performers leaderboard for current month
        /// </summary>
        Task<List<TopPerformerModel>> GetTopPerformersAsync(int count = 10);
    }
}