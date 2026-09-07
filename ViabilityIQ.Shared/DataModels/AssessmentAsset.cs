using Dapper.Contrib.Extensions;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViabilityIQ.Shared.DataModelsInterfaces;

namespace ViabilityIQ.Shared.DataModels
{

    [Dapper.Contrib.Extensions.Table("tblAssessmentAssets")]
    public class AssessmentAsset : IEntity, IAuditableEntity, ISortableEntity
    {

        // ====================================================
        // ASSET IDENTIFICATION
        // ====================================================
        [Dapper.Contrib.Extensions.Key] public long AssessmentAssetId { get; set; }
        public long AssessmentId { get; set; }
        public long AssetTypeId { get; set; }
        [Required(ErrorMessage = "Asset Description is required.")]
        public string? AssetName { get; set; }
        public string AssetReference { get; set; } = string.Empty;   //-- Serial number, license plate, etc.
        public long? AssetCategoryId { get; set; }

        // ====================================================
        // OPENING POSITION (at assessment start date)
        // ====================================================


        /// Gross value of the asset at assessment start
        /// For mid-period acquisitions, this is the acquisition value

        [Column("OpeningBalanceValue")]
        public decimal OpeningBalanceValue { get; set; } = 0;


        /// Accumulated depreciation at assessment start
        /// For mid-period acquisitions, this is $0

        [Column("OpeningAccumulatedDepreciation")]
        public decimal OpeningAccumulatedDepreciation { get; set; } = 0;

        // ====================================================
        // ASSET ACQUISITION DETAILS
        // ====================================================


        /// The month when this asset was acquired during the assessment period (1-12)
        /// 0 = Pre-existing asset (owned at assessment start)
        /// 1-12 = Asset acquired in that specific month during the assessment

        [Column("AcquisitionStartMonth")]
        public int AcquisitionStartMonth { get; set; } = 0;     //This is for existing assets, so default to 0 (pre-existing asset)


        /// Original purchase/acquisition date of the asset        
        [Column("AcquisitionDate")]
        public DateTime? AcquisitionDate { get; set; }


        /// How the asset was acquired (Purchase, Donation, Manufacture, Trade-in, etc.)

        [StringLength(100)]
        [Column("AcquisitionMethod")]
        public string? AcquisitionMethod { get; set; }

        // ====================================================
        // DEPRECIATION & VALUATION
        // ====================================================


        /// Annual depreciation rate as a percentage (e.g., 10 = 10% per year)

        [Column("DepreciationRate")]
        public decimal DepreciationRate { get; set; } = 0;


        /// Method of depreciation (Straight-Line, Declining-Balance, Units-of-Production)

        [StringLength(50)]
        [Column("DepreciationMethod")]
        public string? DepreciationMethod { get; set; }


        /// Useful life of the asset in years

        [Column("UsefulLifeYears")]
        public int? UsefulLifeYears { get; set; }


        /// Basis of valuation (Historical Cost, Fair Value, Replacement Cost)

        [StringLength(50)]
        [Column("ValuationBasis")]
        public string? ValuationBasis { get; set; }


        /// Fair value amount if applicable

        [Column("FairValueAmount")]
        public decimal? FairValueAmount { get; set; }

        // ====================================================
        // ASSET CLASSIFICATION
        // ====================================================


        /// Is this a current asset?

        [Column("IsCurrentAsset")]
        public bool IsCurrentAsset { get; set; } = false;


        /// Is this asset depreciable?

        [Column("IsDepreciable")]
        public bool IsDepreciable { get; set; } = true;


        /// Is this a tangible asset?

        [Column("IsTangible")]
        public bool IsTangible { get; set; } = true;


        /// Has this asset been impaired?

        [Column("IsImpaired")]
        public bool IsImpaired { get; set; } = false;


        /// If impaired, the reason for impairment

        [StringLength(500)]
        [Column("ImpairmentReason")]
        public string? ImpairmentReason { get; set; }

        // ====================================================
        // FINANCING
        // ====================================================


        /// Financing status (Owned, Leased, Financed)

        [StringLength(50)]
        [Column("FinancingStatus")]
        public string? FinancingStatus { get; set; }

        // ====================================================
        // ADDITIONAL DETAILS
        // ====================================================


        /// Asset description or remarks

        [StringLength(2000)]
        [Column("Remarks")]
        public string? Remarks { get; set; }

        // ====================================================
        // AUDIT TRAIL
        // ====================================================



        // ====================================================
        // COMPUTED PROPERTIES (Not Mapped to Database)
        // ====================================================


        /// Is this a pre-existing asset (owned at assessment start)?
        /// True if AcquisitionStartMonth == 0

        [NotMapped] public bool IsPreExisting => AcquisitionStartMonth == 0;


        /// Opening net book value (Gross - Accumulated Depreciation)        
        [NotMapped] public decimal OpeningNetBookValue => OpeningBalanceValue - OpeningAccumulatedDepreciation;


        /// Monthly depreciation amount (Annual Rate / 12)        
        [Write(false)]
        public decimal MonthlyDepreciationAmount
        {
            get
            {
                if (OpeningBalanceValue > 0)
                {
                    return (OpeningBalanceValue * (DepreciationRate / 100m)) / 12m;
                }
                return 0;
            }
        }


        /// Status label for UI display

        [Write(false)]
        public string StatusLabel
        {
            get
            {
                if (IsPreExisting)
                    return "Existing Asset";
                else
                    return $"Acquired - Month {AcquisitionStartMonth}";
            }
        }


        /// Status icon for UI display

        [Write(false)]
        public string StatusIcon
        {
            get
            {
                return IsPreExisting ? "bi-check-circle" : "bi-plus-circle";
            }
        }


        /// Status badge CSS class for UI display

        [Write(false)]
        public string StatusBadgeClass
        {
            get
            {
                return IsPreExisting ? "badge bg-success" : "badge bg-info";
            }
        }

        // ====================================================
        // METHODS
        // ====================================================

        /// Determine if asset is active in a specific month
        /// Pre-existing: always active (returns true for all months 1-12)
        /// Mid-period: active from AcquisitionStartMonth onwards

        public bool IsActiveInMonth(int month)
        {
            if (IsPreExisting)
                return true;

            return month >= AcquisitionStartMonth;
        }


        /// Get display status for a specific month        
        public string GetMonthStatus(int month)
        {
            if (!IsActiveInMonth(month))
                return "Not Owned";

            if (month == AcquisitionStartMonth)
                return "Acquired";

            return "Active";
        }


        /// Get acquisition type description

        public string GetAcquisitionTypeDescription()
        {
            if (IsPreExisting)
                return "Pre-existing asset at assessment start";
            else
                return $"Asset acquired during assessment period (Month {AcquisitionStartMonth})";
        }


        // ====================================================
        // AUDIT & METADATA
        // ====================================================
        public bool Active { get; set; } = true;

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public long CreatedBy { get; set; }
        public DateTime ModifiedDate { get; set; } = DateTime.UtcNow;
        public long ModifiedBy { get; set; }

        long IEntity.Id => AssessmentAssetId;
        string ISortableEntity.DisplayName => AssetName ?? string.Empty;
    }
}
