using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ViabilityIQ.Shared.DataModels;
using ViabilityIQ.Shared.SharedModels;

namespace ViabilityIQ.Application.Interfaces
{
    public interface IProjectionStateManager
    {
        /// This will be FIRED when any projection VARIABLE changes        
        event EventHandler<ProjectionChangedEventArgs> ProjectionChanged;


        /// Gets or calculates loan repayment projection        
        Task<List<AssessmentLoanRepayment>> GetLoanRepaymentProjectionAsync(
            AssessmentLoan loan, LoanCalculationMethodsEnums method, bool forceRecalculate = false);

        /// Invalidates cached data when something changes        
        Task InvalidateDataAsync(string dataType, long entityId, long assessmentId);


        /// Gets cache stats for monitoring        
        ProjectionCacheStats GetCacheStats();


        /// Clears all cache for an assessment        
        Task ClearAssessmentCacheAsync(long assessmentId);
    }


    /// Event args for projection changes    
    public class ProjectionChangedEventArgs : EventArgs
    {
        public string DataType { get; set; }
        public long EntityId { get; set; }          // ✅ Changed from int to long
        public long AssessmentId { get; set; }      // ✅ Changed from int to long
        public HashSet<string> AffectedProjections { get; set; }
        public DateTime ChangedAt { get; set; }
    }


    /// Cache statistics for monitoring

    public class ProjectionCacheStats
    {
        public int TotalCachedItems { get; set; }
        public int ValidCacheItems { get; set; }
        public int ExpiredCacheItems { get; set; }
        public double CacheHitRate { get; set; }
    }
}