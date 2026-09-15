using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ViabilityIQ.Shared.DataModels;

namespace ViabilityIQ.Application.Interfaces
{
    public interface IAssetDepreciationEngine
    {
        
        /// Calculates total depreciation expense for all assets in a month        
        Task<decimal> CalculateMonthDepreciationAsync(long assessmentId, int month);
                
        /// Calculates monthly depreciation for a specific asset        
        decimal CalculateAssetMonthlyDepreciation(AssessmentAsset asset, int month);
                
        /// Gets annual depreciation breakdown by month        
        Task<Dictionary<int, decimal>> CalculateAnnualDepreciationAsync(long assessmentId);
                
        /// Gets depreciation from stored movement records        
        Task<decimal> GetStoredMonthDepreciationAsync(long assessmentAssetId, int month);
    }
}
