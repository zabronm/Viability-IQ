using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViabilityIQ.Shared.DataModels;
using ViabilityIQ.Shared.FinancialModels;


namespace ViabilityIQ.Application.Interfaces
{
    public interface IAssetMovementCalculationService
    {
        
        /// Initializes a new movement template with default values        
        Task<AssessmentAssetMovement> InitializeMovementTemplateAsync(AssessmentAsset asset);
                
        /// Recalculates all 12 months based on current data        
        Task<AssessmentAssetMovement> RecalculateAllMonthsAsync(AssessmentAssetMovement movement, AssessmentAsset asset);
                
        /// Recalculates from a specific month onwards (cascade)        
        Task<AssessmentAssetMovement> RecalculateFromMonthAsync(AssessmentAssetMovement movement, AssessmentAsset asset, int fromMonth);
                
        /// Gets summary of all 12 months for display        
        List<AssetMovementMonthlySummaryDto> GetMonthlySummary(AssessmentAssetMovement movement, AssessmentAsset asset);
                
        /// Gets annual totals and summary        
        AssetMovementAnnualSummaryDto GetAnnualSummary(AssessmentAssetMovement movement, AssessmentAsset asset);
    }
}
