using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ViabilityIQ.Shared.DataModels;
using ViabilityIQ.Shared.FinancialModels;

namespace ViabilityIQ.Application.Interfaces
{
    public interface IAssetRepository
    {
        Task<List<AssessmentAsset>> GetAssessmentAssetsAsync(long assessmentId);
        Task<AssessmentAssetMovement> GetAssetMovementsAsync(long assessmentAssetId);
        Task<List<AssessmentAssetMovement>> GetAllAssetMovementsAsync(long assessmentId);
        Task SaveAssetDepreciationSummaryAsync(long assessmentId, Dictionary<int, decimal> monthlyDepreciation);
        Task<Dictionary<int, decimal>> GetAssetDepreciationSummaryAsync(long assessmentId);
    }
}
