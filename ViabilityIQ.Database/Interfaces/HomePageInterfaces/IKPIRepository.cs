using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViabilityIQ.Web.Models.Dashboard;

namespace ViabilityIQ.Application.Interfaces.HomePageInterfaces
{
    // <summary>
    /// Repository interface for KPI metrics data access
    
    public interface IKPIRepository
    {
        /// <summary>
        /// Get KPI metrics for a specific user
        /// </summary>
        Task<KPIMetricsModel> GetKPIMetricsAsync(long userId);

        /// <summary>
        /// Get KPI metrics for a specific branch
        /// </summary>
        Task<KPIMetricsModel> GetBranchKPIMetricsAsync(int branchId);
    }
}