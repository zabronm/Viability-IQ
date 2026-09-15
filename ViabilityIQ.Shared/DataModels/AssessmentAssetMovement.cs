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
    [Dapper.Contrib.Extensions.Table("tblAssessmentAssetMovement")]
    public class AssessmentAssetMovement : IEntity, IAuditableEntity, ISortableEntity
    {

        // ====================================================
        // PRIMARY KEY & FOREIGN KEYS
        // ====================================================
        /// Primary key for monthly movement record

        [Dapper.Contrib.Extensions.Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long AssessmentAssetMovementId { get; set; }


        /// Foreign key to AssessmentAsset
        /// One-to-One relationship (unique per asset per assessment)        
        [Required] public long AssessmentAssetId { get; set; }

        /// Foreign key to Assessment
        /// For query optimization        
        [Required] public long AssessmentId { get; set; }

        public decimal DepreciationRate { get; set; }
        public string? DepreciationMethod { get; set; }


        // ====================================================
        // MONTH 1 COLUMNS
        // ====================================================

        /// Month 1 - Type of movement (Addition, Disposal, Revaluation, Transfer, or NULL)

        [StringLength(50)]
        [Column("Month_1_MovementType")]
        public string? Month_1_MovementType { get; set; }


        /// Month 1 - Amount of the movement

        [Column("Month_1_MovementValue")]
        public decimal Month_1_MovementValue { get; set; } = 0;


        /// Month 1 - Depreciation expense for the month

        [Column("Month_1_Depreciation")]
        public decimal Month_1_Depreciation { get; set; } = 0;


        /// Month 1 - Calculated ending gross value
        /// Ending Gross = Opening Gross ± Movement

        [Column("Month_1_GrossValue")]
        public decimal? Month_1_GrossValue { get; set; }


        /// Month 1 - Calculated ending accumulated depreciation
        /// Ending Acc. Depr = Opening Acc. Depr + Depreciation

        [Column("Month_1_AccumulatedDepreciation")]
        public decimal? Month_1_AccumulatedDepreciation { get; set; }


        /// Month 1 - Calculated ending net book value
        /// Net = Gross - Accumulated Depreciation

        [Column("Month_1_NetBookValue")]
        public decimal? Month_1_NetBookValue { get; set; }

        // ====================================================
        // MONTH 2 COLUMNS
        // ====================================================

        [StringLength(50)]
        [Column("Month_2_MovementType")]
        public string? Month_2_MovementType { get; set; }

        [Column("Month_2_MovementValue")]
        public decimal Month_2_MovementValue { get; set; } = 0;

        [Column("Month_2_Depreciation")]
        public decimal Month_2_Depreciation { get; set; } = 0;

        [Column("Month_2_GrossValue")]
        public decimal? Month_2_GrossValue { get; set; }

        [Column("Month_2_AccumulatedDepreciation")]
        public decimal? Month_2_AccumulatedDepreciation { get; set; }

        [Column("Month_2_NetBookValue")]
        public decimal? Month_2_NetBookValue { get; set; }

        // ====================================================
        // MONTH 3 COLUMNS
        // ====================================================

        [StringLength(50)]
        [Column("Month_3_MovementType")]
        public string? Month_3_MovementType { get; set; }

        [Column("Month_3_MovementValue")]
        public decimal Month_3_MovementValue { get; set; } = 0;

        [Column("Month_3_Depreciation")]
        public decimal Month_3_Depreciation { get; set; } = 0;

        [Column("Month_3_GrossValue")]
        public decimal? Month_3_GrossValue { get; set; }

        [Column("Month_3_AccumulatedDepreciation")]
        public decimal? Month_3_AccumulatedDepreciation { get; set; }

        [Column("Month_3_NetBookValue")]
        public decimal? Month_3_NetBookValue { get; set; }

        // ====================================================
        // MONTH 4 COLUMNS
        // ====================================================

        [StringLength(50)]
        [Column("Month_4_MovementType")]
        public string? Month_4_MovementType { get; set; }

        [Column("Month_4_MovementValue")]
        public decimal Month_4_MovementValue { get; set; } = 0;

        [Column("Month_4_Depreciation")]
        public decimal Month_4_Depreciation { get; set; } = 0;

        [Column("Month_4_GrossValue")]
        public decimal? Month_4_GrossValue { get; set; }

        [Column("Month_4_AccumulatedDepreciation")]
        public decimal? Month_4_AccumulatedDepreciation { get; set; }

        [Column("Month_4_NetBookValue")]
        public decimal? Month_4_NetBookValue { get; set; }

        // ====================================================
        // MONTH 5 COLUMNS
        // ====================================================

        [StringLength(50)]
        [Column("Month_5_MovementType")]
        public string? Month_5_MovementType { get; set; }

        [Column("Month_5_MovementValue")]
        public decimal Month_5_MovementValue { get; set; } = 0;

        [Column("Month_5_Depreciation")]
        public decimal Month_5_Depreciation { get; set; } = 0;

        [Column("Month_5_GrossValue")]
        public decimal? Month_5_GrossValue { get; set; }

        [Column("Month_5_AccumulatedDepreciation")]
        public decimal? Month_5_AccumulatedDepreciation { get; set; }

        [Column("Month_5_NetBookValue")]
        public decimal? Month_5_NetBookValue { get; set; }

        // ====================================================
        // MONTH 6 COLUMNS
        // ====================================================

        [StringLength(50)]
        [Column("Month_6_MovementType")]
        public string? Month_6_MovementType { get; set; }

        [Column("Month_6_MovementValue")]
        public decimal Month_6_MovementValue { get; set; } = 0;

        [Column("Month_6_Depreciation")]
        public decimal Month_6_Depreciation { get; set; } = 0;

        [Column("Month_6_GrossValue")]
        public decimal? Month_6_GrossValue { get; set; }

        [Column("Month_6_AccumulatedDepreciation")]
        public decimal? Month_6_AccumulatedDepreciation { get; set; }

        [Column("Month_6_NetBookValue")]
        public decimal? Month_6_NetBookValue { get; set; }

        // ====================================================
        // MONTH 7 COLUMNS
        // ====================================================

        [StringLength(50)]
        [Column("Month_7_MovementType")]
        public string? Month_7_MovementType { get; set; }

        [Column("Month_7_MovementValue")]
        public decimal Month_7_MovementValue { get; set; } = 0;

        [Column("Month_7_Depreciation")]
        public decimal Month_7_Depreciation { get; set; } = 0;

        [Column("Month_7_GrossValue")]
        public decimal? Month_7_GrossValue { get; set; }

        [Column("Month_7_AccumulatedDepreciation")]
        public decimal? Month_7_AccumulatedDepreciation { get; set; }

        [Column("Month_7_NetBookValue")]
        public decimal? Month_7_NetBookValue { get; set; }

        // ====================================================
        // MONTH 8 COLUMNS
        // ====================================================

        [StringLength(50)]
        [Column("Month_8_MovementType")]
        public string? Month_8_MovementType { get; set; }

        [Column("Month_8_MovementValue")]
        public decimal Month_8_MovementValue { get; set; } = 0;

        [Column("Month_8_Depreciation")]
        public decimal Month_8_Depreciation { get; set; } = 0;

        [Column("Month_8_GrossValue")]
        public decimal? Month_8_GrossValue { get; set; }

        [Column("Month_8_AccumulatedDepreciation")]
        public decimal? Month_8_AccumulatedDepreciation { get; set; }

        [Column("Month_8_NetBookValue")]
        public decimal? Month_8_NetBookValue { get; set; }

        // ====================================================
        // MONTH 9 COLUMNS
        // ====================================================

        [StringLength(50)]
        [Column("Month_9_MovementType")]
        public string? Month_9_MovementType { get; set; }

        [Column("Month_9_MovementValue")]
        public decimal Month_9_MovementValue { get; set; } = 0;

        [Column("Month_9_Depreciation")]
        public decimal Month_9_Depreciation { get; set; } = 0;

        [Column("Month_9_GrossValue")]
        public decimal? Month_9_GrossValue { get; set; }

        [Column("Month_9_AccumulatedDepreciation")]
        public decimal? Month_9_AccumulatedDepreciation { get; set; }

        [Column("Month_9_NetBookValue")]
        public decimal? Month_9_NetBookValue { get; set; }

        // ====================================================
        // MONTH 10 COLUMNS
        // ====================================================

        [StringLength(50)]
        [Column("Month_10_MovementType")]
        public string? Month_10_MovementType { get; set; }

        [Column("Month_10_MovementValue")]
        public decimal Month_10_MovementValue { get; set; } = 0;

        [Column("Month_10_Depreciation")]
        public decimal Month_10_Depreciation { get; set; } = 0;

        [Column("Month_10_GrossValue")]
        public decimal? Month_10_GrossValue { get; set; }

        [Column("Month_10_AccumulatedDepreciation")]
        public decimal? Month_10_AccumulatedDepreciation { get; set; }

        [Column("Month_10_NetBookValue")]
        public decimal? Month_10_NetBookValue { get; set; }

        // ====================================================
        // MONTH 11 COLUMNS
        // ====================================================

        [StringLength(50)]
        [Column("Month_11_MovementType")]
        public string? Month_11_MovementType { get; set; }

        [Column("Month_11_MovementValue")]
        public decimal Month_11_MovementValue { get; set; } = 0;

        [Column("Month_11_Depreciation")]
        public decimal Month_11_Depreciation { get; set; } = 0;

        [Column("Month_11_GrossValue")]
        public decimal? Month_11_GrossValue { get; set; }

        [Column("Month_11_AccumulatedDepreciation")]
        public decimal? Month_11_AccumulatedDepreciation { get; set; }

        [Column("Month_11_NetBookValue")]
        public decimal? Month_11_NetBookValue { get; set; }

        // ====================================================
        // MONTH 12 COLUMNS
        // ====================================================

        [StringLength(50)]
        [Column("Month_12_MovementType")]
        public string? Month_12_MovementType { get; set; }

        [Column("Month_12_MovementValue")]
        public decimal Month_12_MovementValue { get; set; } = 0;

        [Column("Month_12_Depreciation")]
        public decimal Month_12_Depreciation { get; set; } = 0;

        [Column("Month_12_GrossValue")]
        public decimal? Month_12_GrossValue { get; set; }

        [Column("Month_12_AccumulatedDepreciation")]
        public decimal? Month_12_AccumulatedDepreciation { get; set; }

        [Column("Month_12_NetBookValue")]
        public decimal? Month_12_NetBookValue { get; set; }

        // ====================================================
        // AUDIT TRAIL
        // ====================================================


        [StringLength(2000)]
        [Column("Remarks")]
        public string? Remarks { get; set; }


        /// Date and time when the record was created

        [Required]
        [Column("CreatedDate")]
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;


        /// User ID who created the record

        [Required]
        [Column("CreatedBy")]
        public long CreatedBy { get; set; }


        /// Date and time of last modification

        [Column("ModifiedDate")]
        public DateTime ModifiedDate { get; set; } = DateTime.UtcNow;


        /// User ID who last modified the record

        [Column("ModifiedBy")]
        public long ModifiedBy { get; set; }


        /// Soft delete flag (0 = inactive, 1 = active)

        [Column("Active")]
        public bool Active { get; set; } = true;

        // ====================================================
        // HELPER METHODS
        // ====================================================


        /// Get movement type for a specific month

        public string? GetMovementType(int month)
        {
            return month switch
            {
                1 => Month_1_MovementType,
                2 => Month_2_MovementType,
                3 => Month_3_MovementType,
                4 => Month_4_MovementType,
                5 => Month_5_MovementType,
                6 => Month_6_MovementType,
                7 => Month_7_MovementType,
                8 => Month_8_MovementType,
                9 => Month_9_MovementType,
                10 => Month_10_MovementType,
                11 => Month_11_MovementType,
                12 => Month_12_MovementType,
                _ => null
            };
        }


        /// Get movement value for a specific month

        public decimal GetMovementValue(int month)
        {
            return month switch
            {
                1 => Month_1_MovementValue,
                2 => Month_2_MovementValue,
                3 => Month_3_MovementValue,
                4 => Month_4_MovementValue,
                5 => Month_5_MovementValue,
                6 => Month_6_MovementValue,
                7 => Month_7_MovementValue,
                8 => Month_8_MovementValue,
                9 => Month_9_MovementValue,
                10 => Month_10_MovementValue,
                11 => Month_11_MovementValue,
                12 => Month_12_MovementValue,
                _ => 0
            };
        }


        /// Get depreciation for a specific month

        public decimal GetDepreciation(int month)
        {
            return month switch
            {
                1 => Month_1_Depreciation,
                2 => Month_2_Depreciation,
                3 => Month_3_Depreciation,
                4 => Month_4_Depreciation,
                5 => Month_5_Depreciation,
                6 => Month_6_Depreciation,
                7 => Month_7_Depreciation,
                8 => Month_8_Depreciation,
                9 => Month_9_Depreciation,
                10 => Month_10_Depreciation,
                11 => Month_11_Depreciation,
                12 => Month_12_Depreciation,
                _ => 0
            };
        }


        /// Get net book value for a specific month

        public decimal? GetNetBookValue(int month)
        {
            return month switch
            {
                1 => Month_1_NetBookValue,
                2 => Month_2_NetBookValue,
                3 => Month_3_NetBookValue,
                4 => Month_4_NetBookValue,
                5 => Month_5_NetBookValue,
                6 => Month_6_NetBookValue,
                7 => Month_7_NetBookValue,
                8 => Month_8_NetBookValue,
                9 => Month_9_NetBookValue,
                10 => Month_10_NetBookValue,
                11 => Month_11_NetBookValue,
                12 => Month_12_NetBookValue,
                _ => null
            };
        }


        /// Set movement type for a specific month

        public void SetMovementType(int month, string? value)
        {
            switch (month)
            {
                case 1: Month_1_MovementType = value; break;
                case 2: Month_2_MovementType = value; break;
                case 3: Month_3_MovementType = value; break;
                case 4: Month_4_MovementType = value; break;
                case 5: Month_5_MovementType = value; break;
                case 6: Month_6_MovementType = value; break;
                case 7: Month_7_MovementType = value; break;
                case 8: Month_8_MovementType = value; break;
                case 9: Month_9_MovementType = value; break;
                case 10: Month_10_MovementType = value; break;
                case 11: Month_11_MovementType = value; break;
                case 12: Month_12_MovementType = value; break;
            }
        }


        /// Set movement value for a specific month

        public void SetMovementValue(int month, decimal value)
        {
            switch (month)
            {
                case 1: Month_1_MovementValue = value; break;
                case 2: Month_2_MovementValue = value; break;
                case 3: Month_3_MovementValue = value; break;
                case 4: Month_4_MovementValue = value; break;
                case 5: Month_5_MovementValue = value; break;
                case 6: Month_6_MovementValue = value; break;
                case 7: Month_7_MovementValue = value; break;
                case 8: Month_8_MovementValue = value; break;
                case 9: Month_9_MovementValue = value; break;
                case 10: Month_10_MovementValue = value; break;
                case 11: Month_11_MovementValue = value; break;
                case 12: Month_12_MovementValue = value; break;
            }
        }


        /// Set depreciation for a specific month

        public void SetDepreciation(int month, decimal value)
        {
            switch (month)
            {
                case 1: Month_1_Depreciation = value; break;
                case 2: Month_2_Depreciation = value; break;
                case 3: Month_3_Depreciation = value; break;
                case 4: Month_4_Depreciation = value; break;
                case 5: Month_5_Depreciation = value; break;
                case 6: Month_6_Depreciation = value; break;
                case 7: Month_7_Depreciation = value; break;
                case 8: Month_8_Depreciation = value; break;
                case 9: Month_9_Depreciation = value; break;
                case 10: Month_10_Depreciation = value; break;
                case 11: Month_11_Depreciation = value; break;
                case 12: Month_12_Depreciation = value; break;
            }
        }


        /// Set net book value for a specific month

        public void SetNetBookValue(int month, decimal? value)
        {
            switch (month)
            {
                case 1: Month_1_NetBookValue = value; break;
                case 2: Month_2_NetBookValue = value; break;
                case 3: Month_3_NetBookValue = value; break;
                case 4: Month_4_NetBookValue = value; break;
                case 5: Month_5_NetBookValue = value; break;
                case 6: Month_6_NetBookValue = value; break;
                case 7: Month_7_NetBookValue = value; break;
                case 8: Month_8_NetBookValue = value; break;
                case 9: Month_9_NetBookValue = value; break;
                case 10: Month_10_NetBookValue = value; break;
                case 11: Month_11_NetBookValue = value; break;
                case 12: Month_12_NetBookValue = value; break;
            }
        }

        /// <summary>
        /// Get gross value for a specific month
        /// </summary>
        public decimal? GetGrossValue(int month)
        {
            return month switch
            {
                1 => Month_1_GrossValue,
                2 => Month_2_GrossValue,
                3 => Month_3_GrossValue,
                4 => Month_4_GrossValue,
                5 => Month_5_GrossValue,
                6 => Month_6_GrossValue,
                7 => Month_7_GrossValue,
                8 => Month_8_GrossValue,
                9 => Month_9_GrossValue,
                10 => Month_10_GrossValue,
                11 => Month_11_GrossValue,
                12 => Month_12_GrossValue,
                _ => null
            };
        }

        /// <summary>
        /// Set gross value for a specific month
        /// </summary>
        public void SetGrossValue(int month, decimal? value)
        {
            switch (month)
            {
                case 1: Month_1_GrossValue = value; break;
                case 2: Month_2_GrossValue = value; break;
                case 3: Month_3_GrossValue = value; break;
                case 4: Month_4_GrossValue = value; break;
                case 5: Month_5_GrossValue = value; break;
                case 6: Month_6_GrossValue = value; break;
                case 7: Month_7_GrossValue = value; break;
                case 8: Month_8_GrossValue = value; break;
                case 9: Month_9_GrossValue = value; break;
                case 10: Month_10_GrossValue = value; break;
                case 11: Month_11_GrossValue = value; break;
                case 12: Month_12_GrossValue = value; break;
            }
        }

        /// <summary>
        /// Get accumulated depreciation for a specific month
        /// </summary>
        public decimal? GetAccumulatedDepreciation(int month)
        {
            return month switch
            {
                1 => Month_1_AccumulatedDepreciation,
                2 => Month_2_AccumulatedDepreciation,
                3 => Month_3_AccumulatedDepreciation,
                4 => Month_4_AccumulatedDepreciation,
                5 => Month_5_AccumulatedDepreciation,
                6 => Month_6_AccumulatedDepreciation,
                7 => Month_7_AccumulatedDepreciation,
                8 => Month_8_AccumulatedDepreciation,
                9 => Month_9_AccumulatedDepreciation,
                10 => Month_10_AccumulatedDepreciation,
                11 => Month_11_AccumulatedDepreciation,
                12 => Month_12_AccumulatedDepreciation,
                _ => null
            };
        }

        /// <summary>
        /// Set accumulated depreciation for a specific month
        /// </summary>
        public void SetAccumulatedDepreciation(int month, decimal? value)
        {
            switch (month)
            {
                case 1: Month_1_AccumulatedDepreciation = value; break;
                case 2: Month_2_AccumulatedDepreciation = value; break;
                case 3: Month_3_AccumulatedDepreciation = value; break;
                case 4: Month_4_AccumulatedDepreciation = value; break;
                case 5: Month_5_AccumulatedDepreciation = value; break;
                case 6: Month_6_AccumulatedDepreciation = value; break;
                case 7: Month_7_AccumulatedDepreciation = value; break;
                case 8: Month_8_AccumulatedDepreciation = value; break;
                case 9: Month_9_AccumulatedDepreciation = value; break;
                case 10: Month_10_AccumulatedDepreciation = value; break;
                case 11: Month_11_AccumulatedDepreciation = value; break;
                case 12: Month_12_AccumulatedDepreciation = value; break;
            }
        }

        /// <summary>
        /// Overrides ToString to provide useful debugging information
        /// </summary>
        public override string ToString()
        {
            return $"AssetMovement [AssetId={AssessmentAssetId}, Assessment={AssessmentId}, Created={CreatedDate}, Active={Active}]";
        }

        /// <summary>
        /// Gets all 12 months as an array for batch operations
        /// </summary>
        public decimal[] GetAllGrossValues()
        {
            return new[]
            {
                Month_1_GrossValue ?? 0,
                Month_2_GrossValue ?? 0,
                Month_3_GrossValue ?? 0,
                Month_4_GrossValue ?? 0,
                Month_5_GrossValue ?? 0,
                Month_6_GrossValue ?? 0,
                Month_7_GrossValue ?? 0,
                Month_8_GrossValue ?? 0,
                Month_9_GrossValue ?? 0,
                Month_10_GrossValue ?? 0,
                Month_11_GrossValue ?? 0,
                Month_12_GrossValue ?? 0
            };
        }

        /// <summary>
        /// Gets all 12 months accumulated depreciation as an array
        /// </summary>
        public decimal[] GetAllAccumulatedDepreciation()
        {
            return new[]
            {
                Month_1_AccumulatedDepreciation ?? 0,
                Month_2_AccumulatedDepreciation ?? 0,
                Month_3_AccumulatedDepreciation ?? 0,
                Month_4_AccumulatedDepreciation ?? 0,
                Month_5_AccumulatedDepreciation ?? 0,
                Month_6_AccumulatedDepreciation ?? 0,
                Month_7_AccumulatedDepreciation ?? 0,
                Month_8_AccumulatedDepreciation ?? 0,
                Month_9_AccumulatedDepreciation ?? 0,
                Month_10_AccumulatedDepreciation ?? 0,
                Month_11_AccumulatedDepreciation ?? 0,
                Month_12_AccumulatedDepreciation ?? 0
            };
        }

        /// <summary>
        /// Gets all 12 months depreciation as an array
        /// </summary>
        public decimal[] GetAllDepreciation()
        {
            return new[]
            {
                Month_1_Depreciation,
                Month_2_Depreciation,
                Month_3_Depreciation,
                Month_4_Depreciation,
                Month_5_Depreciation,
                Month_6_Depreciation,
                Month_7_Depreciation,
                Month_8_Depreciation,
                Month_9_Depreciation,
                Month_10_Depreciation,
                Month_11_Depreciation,
                Month_12_Depreciation
            };
        }

        /// <summary>
        /// Gets all 12 months net book values as an array
        /// </summary>
        public decimal?[] GetAllNetBookValues()
        {
            return new[]
            {
                Month_1_NetBookValue,
                Month_2_NetBookValue,
                Month_3_NetBookValue,
                Month_4_NetBookValue,
                Month_5_NetBookValue,
                Month_6_NetBookValue,
                Month_7_NetBookValue,
                Month_8_NetBookValue,
                Month_9_NetBookValue,
                Month_10_NetBookValue,
                Month_11_NetBookValue,
                Month_12_NetBookValue
            };
        }

        /// <summary>
        /// Gets all 12 months movement values as an array
        /// </summary>
        public decimal[] GetAllMovementValues()
        {
            return new[]
            {
                Month_1_MovementValue,
                Month_2_MovementValue,
                Month_3_MovementValue,
                Month_4_MovementValue,
                Month_5_MovementValue,
                Month_6_MovementValue,
                Month_7_MovementValue,
                Month_8_MovementValue,
                Month_9_MovementValue,
                Month_10_MovementValue,
                Month_11_MovementValue,
                Month_12_MovementValue
            };
        }

        /// <summary>
        /// Gets all 12 months movement types as an array
        /// </summary>
        public string?[] GetAllMovementTypes()
        {
            return new[]
            {
                Month_1_MovementType,
                Month_2_MovementType,
                Month_3_MovementType,
                Month_4_MovementType,
                Month_5_MovementType,
                Month_6_MovementType,
                Month_7_MovementType,
                Month_8_MovementType,
                Month_9_MovementType,
                Month_10_MovementType,
                Month_11_MovementType,
                Month_12_MovementType
            };
        }

        /// <summary>
        /// Clears all movement data (resets to defaults)
        /// Useful for reinitializing
        /// </summary>
        public void ClearAllMovementData()
        {
            for (int month = 1; month <= 12; month++)
            {
                SetMovementType(month, null);
                SetMovementValue(month, 0);
                SetDepreciation(month, 0);
                SetGrossValue(month, 0);
                SetAccumulatedDepreciation(month, 0);
                SetNetBookValue(month, 0);
            }
        }

        /// <summary>
        /// Calculates total depreciation for the year
        /// </summary>
        public decimal GetTotalAnnualDepreciation()
        {
            return GetAllDepreciation().Sum();
        }

        /// <summary>
        /// Calculates total movements for the year
        /// </summary>
        public decimal GetTotalAnnualMovements()
        {
            return GetAllMovementValues().Sum();
        }

        /// <summary>
        /// Gets the ending net book value (Month 12)
        /// </summary>
        public decimal? GetEndingNetBookValue()
        {
            return GetNetBookValue(12);
        }

        /// <summary>
        /// Gets the ending gross value (Month 12)
        /// </summary>
        public decimal? GetEndingGrossValue()
        {
            return GetGrossValue(12);
        }

        /// <summary>
        /// Gets the ending accumulated depreciation (Month 12)
        /// </summary>
        public decimal? GetEndingAccumulatedDepreciation()
        {
            return GetAccumulatedDepreciation(12);
        }

        long IEntity.Id => AssessmentAssetMovementId;
        string ISortableEntity.DisplayName => Remarks ?? string.Empty;
    }
}