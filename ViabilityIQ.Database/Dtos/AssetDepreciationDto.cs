using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ViabilityIQ.Application.Dtos
{
    
    /// AssetDepreciationDto - Depreciation schedule and audit trail
    /// Used for displaying depreciation charges over time in UI components
    
    public class AssetDepreciationDto
    {
        // ====================================================
        // PRIMARY IDENTIFIERS
        // ====================================================
        public long AssetDepreciationId { get; set; }
        public long AssessmentAssetId { get; set; }
        public long AssessmentId { get; set; }

        // ====================================================
        // DISPLAY PROPERTIES
        // ====================================================
        
        /// Asset name for display (loaded from related Asset)        
        public string? AssetName { get; set; }

        // ====================================================
        // DEPRECIATION DETAILS
        // ====================================================
        
        /// Date when depreciation was recorded/calculated        
        public DateTime DepreciationDate { get; set; }
        
        /// The amount of depreciation charge for this period        
        public decimal DepreciationAmount { get; set; }
        
        /// Total accumulated depreciation BEFORE this charge        
        public decimal AccumulatedDepreciationBefore { get; set; }

        
        /// Total accumulated depreciation AFTER this charge        
        public decimal AccumulatedDepreciationAfter { get; set; }
        
        /// Depreciation method used: "Straight-Line", "Declining-Balance", "Units-of-Production"        
        public string? Method { get; set; }

        
        /// Additional notes about the depreciation calculation        
        public string? Remarks { get; set; }
        public bool Active { get; set; } = true;

        // ====================================================
        // AUDIT TRAIL
        // ====================================================
        public DateTime CreatedDate { get; set; }
        public long CreatedBy { get; set; }
        public DateTime? ModifiedDate { get; set; }
        public long? ModifiedBy { get; set; }

        // ====================================================
        // COMPUTED PROPERTIES FOR UI DISPLAY
        // ====================================================
        
        /// Formatted depreciation amount for display (e.g., "-$10,000")        
        public string FormattedDepreciation => DepreciationAmount.ToString("C0");
        
        /// Formatted accumulated depreciation before (e.g., "$50,000")        
        public string FormattedAccumulatedBefore => AccumulatedDepreciationBefore.ToString("C0");
        
        /// Formatted accumulated depreciation after (e.g., "$60,000")        
        public string FormattedAccumulatedAfter => AccumulatedDepreciationAfter.ToString("C0");

        
        /// Percentage change in accumulated depreciation        
        public decimal PercentageIncrease
        {
            get
            {
                if (AccumulatedDepreciationBefore == 0 && DepreciationAmount > 0)
                    return 100; // First depreciation charge
                if (AccumulatedDepreciationBefore == 0)
                    return 0;
                return (DepreciationAmount / AccumulatedDepreciationBefore) * 100;
            }
        }
        
        /// Display label for depreciation method        
        public string MethodDisplay
        {
            get
            {
                return Method switch
                {
                    "Straight-Line" => "Straight-Line (Constant)",
                    "Declining-Balance" => "Declining-Balance (Accelerated)",
                    "Units-of-Production" => "Units-of-Production (Usage-Based)",
                    _ => Method ?? "N/A"
                };
            }
        }
        
        /// Summary text for display
        /// E.g., "Depreciation of $10,000 on 2024-01-31 using Straight-Line method"        
        public string Summary
        {
            get
            {
                return $"Depreciation of {DepreciationAmount:C0} on {DepreciationDate:MMM dd, yyyy} using {MethodDisplay}";
            }
        }
        
        /// Indicates if this is the first depreciation charge        
        public bool IsFirstCharge => AccumulatedDepreciationBefore == 0 && DepreciationAmount > 0;
        
        /// Indicates significant depreciation (>20% increase in accumulated)        
        public bool IsSignificantCharge => PercentageIncrease > 20;
    }

}

