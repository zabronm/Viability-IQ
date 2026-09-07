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
       
        /// Get comprehensive insights data for a user
       
        Task<InsightsModel> GetInsightsAsync(long userId);

       
        /// Get insights for a specific branch (admin/manager only)
       
        Task<InsightsModel> GetBranchInsightsAsync(int branchId);

       
        /// Get top performers leaderboard for current month
       
        Task<List<TopPerformerModel>> GetTopPerformersAsync(int count = 10);
    }
}