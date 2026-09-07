using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ViabilityIQ.Application.Interfaces;
using ViabilityIQ.Infrastructure.Repositories;
using ViabilityIQ.Shared.DataModels;
using ViabilityIQ.Shared.FinancialModels;
using ViabilityIQ.Shared.SharedModels;
using ViabilityIQ.Web.Services;

namespace ViabilityIQ.Web.Components.Pages_Assessments.AssetFormComponents
{
    /// <summary>
    /// AssetMovementFormComponent - Component to load, view, and save 12-month asset movements 
    /// with automatic recalculation of gross value, depreciation, and net book value.
    /// </summary>
    public partial class AssetMovementFormComponent : ComponentBase
    {
        // ====================================================
        // INJECTED DEPENDENCIES
        // ====================================================
        [Inject]
        private IGenericDataRepository<AssessmentAsset> AssetRepository { get; set; } = default!;

        [Inject]
        private IGenericDataRepository<AssessmentAssetMovement> MovementRepository { get; set; } = default!;

        [Inject]
        private IGenericDataRepository<AssetType> AssetClassRepository { get; set; } = default!;

        [Inject]
        private MasterDataService MasterDataService { get; set; } = default!;

        [Inject]
        private ZabOffCanvasService? zabCanvasService { get; set; }

        [Inject]
        private ISessionService? sessionService { get; set; }

        [Inject]
        private IProjectionStateManager? projectionStateManager { get; set; }

        [Inject]
        private ToastService? _Toast { get; set; }

        [Inject]
        private ILogger<AssetMovementFormComponent>? Logger { get; set; }

        // ====================================================
        // PARAMETERS
        // ====================================================
        [Parameter]
        public long AssessmentId { get; set; }

        [Parameter]
        public long AssessmentAssetId { get; set; }

        // ====================================================
        // PRIVATE FIELDS - FORM DATA
        // ====================================================
        private AssessmentAssetMovement MovementModel = new();
        private AssessmentAsset? Asset;
        private AssetType? AssetClassModel;
        private string? AssetClassName;
        private string? AssetCategoryName;
        private List<AssetMovementMonthlySummaryDto> MonthlySummary = new();
        private bool IsSubmitting { get; set; }

        // ====================================================
        // LIFECYCLE
        // ====================================================
        protected override async Task OnParametersSetAsync()
        {
            try
            {
                await LoadMonthlyMovements();
            }
            catch (Exception ex)
            {
                Logger?.LogError(ex, "Error loading asset movements for AssessmentAssetId {AssessmentAssetId}", AssessmentAssetId);
                _Toast?.ShowError($"Error loading monthly movements: {ex.Message}", "Error");
            }
        }

        // ====================================================
        // DATA LOADING METHODS
        // ====================================================
        private async Task LoadMonthlyMovements()
        {
            Logger?.LogDebug("Loading monthly asset movements for AssessmentAssetId {AssessmentAssetId}", AssessmentAssetId);

            try
            {
                if (AssessmentAssetId > 0)
                {
                    // Load Asset Master Data
                    var assets = await AssetRepository.GetAllAsync(a => a.AssessmentAssetId == AssessmentAssetId);
                    Asset = assets?.FirstOrDefault();

                    if (Asset != null)
                    {
                        Logger?.LogDebug("Asset loaded: {AssetName}", Asset.AssetName);

                        // ✅ Load Asset Class using repository
                        try
                        {
                            var assetTypes = await AssetClassRepository.GetAllAsync(ac => ac.AssetTypeId == Asset.AssetTypeId);
                            AssetClassModel = assetTypes?.FirstOrDefault();

                            if (AssetClassModel != null)
                            {
                                AssetClassName = AssetClassModel.TypeName;
                                Logger?.LogDebug("Asset Class loaded: {AssetClassName}", AssetClassName);

                                // ✅ Load Asset Category from AssetType model using MasterDataService
                                try
                                {
                                    AssetCategoryName = await MasterDataService.LookAsync<string>(
                                        "tblAssetCategory",
                                        "AssetCategoryName",
                                        "AssetCategoryId",
                                        AssetClassModel.AssetCategoryId);

                                    Logger?.LogDebug("Asset Category loaded: {AssetCategoryName}", AssetCategoryName);
                                }
                                catch (Exception ex)
                                {
                                    Logger?.LogWarning(ex, "Error loading Asset Category for AssetCategoryId {AssetCategoryId}",
                                        AssetClassModel.AssetCategoryId);
                                    AssetCategoryName = "N/A";
                                }
                            }
                            else
                            {
                                AssetClassName = "N/A";
                                AssetCategoryName = "N/A";
                            }
                        }
                        catch (Exception ex)
                        {
                            Logger?.LogWarning(ex, "Error loading Asset Type for AssetTypeId {AssetTypeId}", Asset.AssetTypeId);
                            AssetClassName = "N/A";
                            AssetCategoryName = "N/A";
                        }
                    }

                    // Load Movement Record
                    var movementsList = await MovementRepository.GetAllAsync(x =>
                        x.AssessmentAssetId == AssessmentAssetId);

                    MovementModel = movementsList?.FirstOrDefault() ?? new AssessmentAssetMovement
                    {
                        AssessmentAssetId = AssessmentAssetId,
                        AssessmentId = AssessmentId,
                        CreatedDate = DateTime.UtcNow,
                        CreatedBy = sessionService?.UserId ?? 0
                    };

                    // ✅ Recalculate all months based on asset data
                    if (Asset != null)
                    {
                        RecalculateAllMonths();
                    }
                }
                else
                {
                    MovementModel = new AssessmentAssetMovement
                    {
                        AssessmentId = AssessmentId,
                        CreatedDate = DateTime.UtcNow,
                        CreatedBy = sessionService?.UserId ?? 0
                    };
                }

                StateHasChanged();
            }
            catch (Exception ex)
            {
                Logger?.LogError(ex, "Error in LoadMonthlyMovements");
                throw;
            }
        }

        // ====================================================
        // CALCULATION METHODS
        // ====================================================
        /// <summary>
        /// Recalculates all 12 months based on asset data and movements
        /// </summary>
        private void RecalculateAllMonths()
        {
            if (Asset == null)
                return;

            MonthlySummary = new List<AssetMovementMonthlySummaryDto>();

            Logger?.LogDebug("Recalculating all months for asset {AssetId}", AssessmentAssetId);

            decimal grossValue = Asset.OpeningBalanceValue;
            decimal accumulatedDepreciation = Asset.IsPreExisting ? Asset.OpeningAccumulatedDepreciation : 0;

            for (int month = 1; month <= 12; month++)
            {
                // Get movement for this month
                var movementType = MovementModel.GetMovementType(month);
                var movementValue = MovementModel.GetMovementValue(month);

                // ✅ Apply movement to gross value
                if (!string.IsNullOrEmpty(movementType) && movementValue != 0)
                {
                    switch (movementType)
                    {
                        case "Addition":
                            grossValue += movementValue;
                            Logger?.LogDebug("Month {Month}: Addition of {Value}, Gross Value now: {GrossValue}",
                                month, movementValue, grossValue);
                            break;
                        case "Disposal":
                            grossValue -= movementValue;
                            Logger?.LogDebug("Month {Month}: Disposal of {Value}, Gross Value now: {GrossValue}",
                                month, movementValue, grossValue);
                            break;
                        case "Revaluation":
                            grossValue += movementValue;
                            Logger?.LogDebug("Month {Month}: Revaluation of {Value}, Gross Value now: {GrossValue}",
                                month, movementValue, grossValue);
                            break;
                        case "Transfer":
                            // Transfer doesn't affect gross value
                            break;
                    }
                }

                // ✅ Calculate depreciation for this month
                decimal monthlyDepreciation = 0;
                if (Asset.IsDepreciable && Asset.DepreciationRate != null && Asset.DepreciationRate > 0)
                {
                    // Check if asset is active in this month
                    bool isActive = Asset.IsPreExisting || month >= Asset.AcquisitionStartMonth;

                    if (isActive && grossValue > 0)
                    {
                        // Straight-line depreciation: (Gross Value × Annual Rate) / 12
                        monthlyDepreciation = (grossValue * (Asset.DepreciationRate / 100m)) / 12m;
                    }
                }

                // ✅ Update accumulated depreciation
                accumulatedDepreciation += monthlyDepreciation;

                // ✅ Calculate net book value
                decimal netBookValue = grossValue - accumulatedDepreciation;
                if (netBookValue < 0)
                    netBookValue = 0;

                // Add to summary
                MonthlySummary.Add(new AssetMovementMonthlySummaryDto
                {
                    Month = month,
                    GrossValue = grossValue,
                    Depreciation = monthlyDepreciation,
                    AccumulatedDepreciation = accumulatedDepreciation,
                    NetBookValue = netBookValue
                });

                Logger?.LogDebug(
                    "Month {Month}: Gross={Gross}, Depr={Depr}, AccumDepr={AccumDepr}, NBV={NBV}",
                    month, grossValue, monthlyDepreciation, accumulatedDepreciation, netBookValue);
            }
        }

        // ====================================================
        // EVENT HANDLERS & BINDING HELPERS
        // ====================================================
        private void OnMovementTypeChanged(int month, string? value)
        {
            try
            {
                Logger?.LogDebug("Movement type changed for month {Month}: {MovementType}", month, value);

                MovementModel.SetMovementType(month, value);

                // ✅ Recalculate all months
                RecalculateAllMonths();

                StateHasChanged();
            }
            catch (Exception ex)
            {
                Logger?.LogError(ex, "Error changing movement type for month {Month}", month);
                _Toast?.ShowError($"Error updating movement type: {ex.Message}", "Error");
            }
        }

        private void OnMovementValueChanged(int month, object? rawValue)
        {
            try
            {
                if (decimal.TryParse(rawValue?.ToString(), out decimal val))
                {
                    Logger?.LogDebug("Movement value changed for month {Month}: {Value}", month, val);

                    MovementModel.SetMovementValue(month, val);

                    // ✅ Recalculate all months - this will cascade changes to net book value
                    RecalculateAllMonths();

                    StateHasChanged();
                }
            }
            catch (Exception ex)
            {
                Logger?.LogError(ex, "Error changing movement value for month {Month}", month);
                _Toast?.ShowError($"Error updating movement value: {ex.Message}", "Error");
            }
        }

        private async Task ExecuteSaveWorkflowAsync()
        {
            if (IsSubmitting)
                return;

            IsSubmitting = true;
            StateHasChanged();

            try
            {
                if (AssessmentAssetId <= 0)
                {
                    _Toast?.ShowError("Invalid Asset reference associated with movements.", "Validation Error");
                    IsSubmitting = false;
                    StateHasChanged();
                    return;
                }

                MovementModel.AssessmentAssetId = AssessmentAssetId;
                MovementModel.AssessmentId = AssessmentId;
                MovementModel.ModifiedDate = DateTime.UtcNow;
                MovementModel.ModifiedBy = sessionService?.UserId ?? 0;

                Logger?.LogInformation("Saving asset movement records for AssessmentAssetId {AssessmentAssetId}", AssessmentAssetId);

                // ✅ Save movement record (automatic cache invalidation via CacheInvalidationInterceptor)
                await MovementRepository.SaveAsync(MovementModel);

                Logger?.LogInformation(
                    "Asset movement saved successfully for AssessmentAssetId {AssessmentAssetId}",
                    AssessmentAssetId);

                _Toast?.ShowSuccess("Asset movements saved successfully!", "Success");

                await Task.Delay(300);
                await zabCanvasService!.PublishResultAsync(SaveResult.SavedAndClose("Asset movements updated successfully."));
            }
            catch (Exception ex)
            {
                Logger?.LogError(ex, "Error saving asset movements for AssessmentAssetId {AssessmentAssetId}", AssessmentAssetId);
                _Toast?.ShowError($"Error saving movements: {ex.Message}", "Error");
            }
            finally
            {
                IsSubmitting = false;
                StateHasChanged();
            }
        }

        private async Task CancelFormAsync()
        {
            try
            {
                await zabCanvasService!.HideAsync(SaveResult.Cancel());
            }
            catch (Exception ex)
            {
                Logger?.LogError(ex, "Error cancelling asset movement form");
            }
        }
    }
}