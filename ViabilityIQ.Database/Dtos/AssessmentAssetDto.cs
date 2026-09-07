using Dapper.Contrib.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ViabilityIQ.Application.Dtos
{
    
    /// AssessmentAssetDto - Maps to vwAssessmentAssets view
    /// Provides denormalized asset data with related category and type information
    
    [Table("vw_assessment_assets_list")]  // ✅ Points to SQL view
    public class AssessmentAssetDto
    {
        // ====================================================
        // ASSET IDENTIFICATION
        // ====================================================
        public long AssessmentAssetId { get; set; }
        public long AssessmentId { get; set; }
        public string? AssetName { get; set; }
        public string? Remarks { get; set; }
        public string? AssetReference { get; set; }
        public bool PreExisting { get; set; } = true;
        // ====================================================
        // CATEGORY FIELDS
        // ====================================================
        public long? AssetCategoryId { get; set; }
        public string? AssetCategoryName { get; set; }
        public string? BalanceSheetSection { get; set; }

        // ====================================================
        // TYPE FIELDS
        // ====================================================
        public long AssetTypeId { get; set; }
        public string? AssetTypeName { get; set; }
        public decimal DefaultDepreciationRate { get; set; }

        // ====================================================
        // OPENING BALANCE
        // ====================================================
        public decimal OpeningBalanceValue { get; set; }
        public decimal OpeningAccumulatedDepreciation { get; set; }
        public decimal OpeningNetValue { get; set; }

        // ====================================================
        // CLOSING BALANCE
        // ====================================================
        public decimal ClosingBalanceValue { get; set; }
        public decimal ClosingAccumulatedDepreciation { get; set; }
        public decimal ClosingNetBookValue { get; set; }

        // ====================================================
        // DEPRECIATION
        // ====================================================
        public string? DepreciationMethod { get; set; }
        public decimal DepreciationRate { get; set; }
        public int? UsefulLifeYears { get; set; }

        // ====================================================
        // MOVEMENTS
        // ====================================================
        public decimal AdditionsValue { get; set; }
        public decimal DisposalsValue { get; set; }
        public decimal DepreciationExpenseAmount { get; set; }


        // ====================================================
        // MOVEMENTS
        // ====================================================
        public decimal OpeningNetBookValue { get; set; }
        public decimal TotalAdditions { get; set; }
        public decimal TotalDisposals { get; set; }
        public decimal TotalDepreciation { get; set; }       
        public int AssetLife { get; set; }       


        // ====================================================
        // CLASSIFICATION
        // ====================================================
        public bool IsCurrentAsset { get; set; }
        public bool IsDepreciable { get; set; }
        public bool IsTangible { get; set; }

        // ====================================================
        // VALUATION & IMPAIRMENT
        // ====================================================
        public bool IsImpaired { get; set; }
        public string? ImpairmentReason { get; set; }
        public decimal? FairValueAmount { get; set; }
        public string? ValuationBasis { get; set; }

        // ====================================================
        // AUDIT TRAIL
        // ====================================================
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public long CreatedBy { get; set; }
        public DateTime ModifiedDate { get; set; } = DateTime.UtcNow;
        public long ModifiedBy { get; set; }
        public bool Active { get; set; }
    }
}
