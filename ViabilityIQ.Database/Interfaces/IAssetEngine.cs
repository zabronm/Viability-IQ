using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViabilityIQ.Application.FinancialCalculations;
using ViabilityIQ.Shared.DataModels;

namespace ViabilityIQ.Application.Interfaces
{
    public interface IAssetEngine
    {
        Task<Dictionary<int, decimal>> CalculateTotalAssetDepreciationAsync(long assessmentId);
        Task<Dictionary<int, decimal>> CalculateAssetDepreciationAsync(AssessmentAsset asset);
        Task<Dictionary<int, decimal>> CalculateTotalAssetGrossValueAsync(long assessmentId);
        Task<Dictionary<int, decimal>> CalculateAssetGrossValueAsync(AssessmentAsset asset);
        Task<List<AssetProjectionDto>> GetAllAssetsWithProjectionsAsync(long assessmentId);
        Task<bool> RecalculateAssetProjectionsAsync(long assessmentId);
    }
}
