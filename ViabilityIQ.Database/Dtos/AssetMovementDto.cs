using System;

namespace ViabilityIQ.Application.Dtos
{
    /// <summary>
    /// AssetMovementDto - Data Transfer Object for asset movements
    /// Represents a single month's movement data for an asset within a 12-month projection
    /// Maps to AssessmentAssetMovement model which uses Month_N_ column naming convention
    /// </summary>
    public class AssetMovementDto
    {
        // ====================================================
        // PRIMARY IDENTIFIERS
        // ====================================================

        /// <summary>
        /// The Assessment Asset ID this movement belongs to
        /// </summary>
        public long AssessmentAssetId { get; set; }

        /// <summary>
        /// The Assessment ID (for context and relationship)
        /// </summary>
        public long AssessmentId { get; set; }

        /// <summary>
        /// The month number (1-12) this movement represents
        /// </summary>
        public int Month { get; set; }

        // ====================================================
        // DISPLAY PROPERTIES
        // ====================================================

        /// <summary>
        /// Asset name for display (loaded from related Asset)
        /// </summary>
        public string? AssetName { get; set; }

        /// <summary>
        /// Asset type for display
        /// </summary>
        public string? AssetType { get; set; }

        // ====================================================
        // MOVEMENT DATA - Direct from Model
        // ====================================================

        /// <summary>
        /// Type of movement: "Addition", "Disposal", "Transfer", "Revaluation"
        /// NULL or empty string = No movement for this month
        /// Maps to: Month_N_MovementType
        /// </summary>
        public string? MovementType { get; set; }

        /// <summary>
        /// The amount of the movement (can be positive or negative)
        /// Maps to: Month_N_MovementValue
        /// </summary>
        public decimal MovementValue { get; set; } = 0;

        /// <summary>
        /// Monthly depreciation expense for this month
        /// Maps to: Month_N_Depreciation
        /// </summary>
        public decimal Depreciation { get; set; } = 0;

        // ====================================================
        // CALCULATED VALUES - Direct from Model
        // ====================================================

        /// <summary>
        /// Ending gross value for this month
        /// Calculated as: Previous Gross ± Movement
        /// Maps to: Month_N_GrossValue
        /// </summary>
        public decimal? GrossValue { get; set; }

        /// <summary>
        /// Ending accumulated depreciation for this month
        /// Calculated as: Previous Accumulated Depreciation + Current Depreciation
        /// Maps to: Month_N_AccumulatedDepreciation
        /// </summary>
        public decimal? AccumulatedDepreciation { get; set; }

        /// <summary>
        /// Ending net book value for this month
        /// Calculated as: Gross Value - Accumulated Depreciation
        /// Maps to: Month_N_NetBookValue
        /// </summary>
        public decimal? NetBookValue { get; set; }

        // ====================================================
        // AUDIT TRAIL - For tracking changes
        // ====================================================

        /// <summary>
        /// Date and time when the record was created
        /// </summary>
        public DateTime CreatedDate { get; set; }

        /// <summary>
        /// User ID who created the record
        /// </summary>
        public long CreatedBy { get; set; }

        /// <summary>
        /// Date and time of last modification
        /// </summary>
        public DateTime? ModifiedDate { get; set; }

        /// <summary>
        /// User ID who last modified the record
        /// </summary>
        public long? ModifiedBy { get; set; }

        /// <summary>
        /// Record active status
        /// </summary>
        public bool Active { get; set; } = true;

        // ====================================================
        // COMPUTED PROPERTIES FOR UI DISPLAY
        // ====================================================

        /// <summary>
        /// Display label for movement type with icon
        /// E.g., "➕ Addition", "➖ Disposal", "↔️ Transfer", "📊 Revaluation"
        /// </summary>
        public string MovementTypeDisplay
        {
            get
            {
                if (string.IsNullOrEmpty(MovementType))
                    return "— No Movement";

                return MovementType switch
                {
                    "Addition" => "➕ Addition",
                    "Disposal" => "➖ Disposal",
                    "Transfer" => "↔️ Transfer",
                    "Revaluation" => "📊 Revaluation",
                    _ => MovementType
                };
            }
        }

        /// <summary>
        /// CSS color class for movement type badge
        /// </summary>
        public string MovementTypeColor
        {
            get
            {
                return MovementType switch
                {
                    "Addition" => "text-success",
                    "Disposal" => "text-danger",
                    "Transfer" => "text-info",
                    "Revaluation" => "text-warning",
                    _ => "text-dark"
                };
            }
        }

        /// <summary>
        /// CSS background class for movement type badge
        /// </summary>
        public string MovementTypeBadgeClass
        {
            get
            {
                return MovementType switch
                {
                    "Addition" => "bg-success text-white",
                    "Disposal" => "bg-danger text-white",
                    "Transfer" => "bg-info text-black",
                    "Revaluation" => "bg-warning text-black",
                    _ => "bg-secondary text-white"
                };
            }
        }

        /// <summary>
        /// Formatted movement value for display
        /// Shows positive amounts with "+" prefix and negative with "-" prefix
        /// E.g., "+$50,000" or "-$10,000"
        /// </summary>
        public string FormattedMovementValue
        {
            get
            {
                if (MovementValue == 0)
                    return "$0";

                if (MovementValue > 0)
                    return $"+{MovementValue:C0}";

                return MovementValue.ToString("C0");
            }
        }

        /// <summary>
        /// Absolute value of movement formatted as currency
        /// E.g., "$50,000"
        /// </summary>
        public string FormattedAbsoluteMovement => Math.Abs(MovementValue).ToString("C0");

        /// <summary>
        /// Formatted depreciation amount
        /// </summary>
        public string FormattedDepreciation => Depreciation.ToString("C0");

        /// <summary>
        /// Formatted gross value (ending)
        /// </summary>
        public string FormattedGrossValue => GrossValue?.ToString("C0") ?? "$0";

        /// <summary>
        /// Formatted accumulated depreciation (ending)
        /// </summary>
        public string FormattedAccumulatedDepreciation => AccumulatedDepreciation?.ToString("C0") ?? "$0";

        /// <summary>
        /// Formatted net book value (ending)
        /// </summary>
        public string FormattedNetBookValue => NetBookValue?.ToString("C0") ?? "$0";

        /// <summary>
        /// Indicates if there is a movement in this month
        /// </summary>
        public bool HasMovement => !string.IsNullOrEmpty(MovementType) && MovementValue != 0;

        /// <summary>
        /// Indicates if movement is an addition (positive amount)
        /// </summary>
        public bool IsAddition => MovementType == "Addition" && MovementValue > 0;

        /// <summary>
        /// Indicates if movement is a disposal (negative amount)
        /// </summary>
        public bool IsDisposal => MovementType == "Disposal" && MovementValue < 0;

        /// <summary>
        /// Indicates if movement is a transfer (typically zero value)
        /// </summary>
        public bool IsTransfer => MovementType == "Transfer";

        /// <summary>
        /// Indicates if movement is a revaluation
        /// </summary>
        public bool IsRevaluation => MovementType == "Revaluation";

        /// <summary>
        /// Indicates if this is a significant movement (absolute value > 5000)
        /// </summary>
        public bool IsSignificantMovement => Math.Abs(MovementValue) > 5000;

        /// <summary>
        /// Month display label (e.g., "Month 1", "Month 12")
        /// </summary>
        public string MonthDisplay => $"Month {Month}";

        /// <summary>
        /// Summary text for display
        /// E.g., "Addition of $50,000"
        /// </summary>
        public string Summary
        {
            get
            {
                if (!HasMovement)
                    return "No movement";

                return $"{MovementTypeDisplay} of {FormattedMovementValue}";
            }
        }

        /// <summary>
        /// Status indicating if asset is active in this month
        /// Used to display greyed-out rows for mid-period acquisitions
        /// Returns: "Not Owned", movement type, or "Active"
        /// </summary>
        public string MonthStatus
        {
            get
            {
                if (GrossValue == 0 && AccumulatedDepreciation == 0)
                    return "Not Owned";

                if (!string.IsNullOrEmpty(MovementType))
                    return MovementType;

                return "Active";
            }
        }

        /// <summary>
        /// Indicates if asset is active (has values) in this month
        /// </summary>
        public bool IsMonthActive => GrossValue.HasValue && GrossValue > 0;
    }
}