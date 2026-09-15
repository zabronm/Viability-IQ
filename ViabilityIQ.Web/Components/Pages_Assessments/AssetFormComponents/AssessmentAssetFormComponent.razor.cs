using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using ViabilityIQ.Application.Interfaces;
using ViabilityIQ.Infrastructure.Repositories;
using ViabilityIQ.Shared.DataModels;
using ViabilityIQ.Shared.SharedModels;
using ViabilityIQ.Web.Services;


namespace ViabilityIQ.Web.Components.Pages_Assessments.AssetFormComponents
{
    
    /// AssessmentAssetFormComponent - Form for creating/editing assessment assets
    /// Supports both pre-existing and mid-period acquired assets
    
    public partial class AssessmentAssetFormComponent : ComponentBase
    {
        // ====================================================
        // INJECTED DEPENDENCIES
        // ====================================================
        [Inject]        private IGenericDataRepository<AssessmentAsset> DataRepository { get; set; } = default!;
        [Inject]        private IGenericDataRepository<AssessmentAssetMovement> MovementRepository { get; set; } = default!;
        [Inject]        private IAssetMovementCalculationService? movementCalculationService { get; set; }
        [Inject]        private IFinancialCalculationsEngine? financialCalculationsEngine { get; set; }        
        [Inject]        private ZabOffCanvasService? zabCanvasService { get; set; }
        [Inject]        private ISessionService? sessionService { get; set; }
        [Inject]        private IProjectionStateManager? projectionStateManager { get; set; }
        [Inject]        private ToastService? _Toast { get; set; }
        [Inject]        private ILogger<AssessmentAssetFormComponent>? Logger { get; set; }
     
        // ====================================================
        // PARAMETERS
        // ====================================================
        
        /// The Assessment ID
        
        [Parameter]
        public long AssessmentId { get; set; }

        
        /// The Assessment Asset ID (for edit mode)
        /// 0 or omitted = Create new asset
        
        [Parameter]
        public long AssessmentAssetId { get; set; }

        // ====================================================
        // PRIVATE FIELDS - FORM DATA
        // ====================================================
        private AssessmentAsset FormModel = new();
        private bool IsSubmitting { get; set; }

        // ====================================================
        // LIFECYCLE
        // ====================================================
        protected override async Task OnParametersSetAsync()
        {
            try
            {
                await LoadAsset();
            }
            catch (Exception ex)
            {
                Logger?.LogError(ex, "Error loading asset for AssessmentAssetId {AssessmentAssetId}", AssessmentAssetId);
                _Toast?.ShowError($"Error loading asset: {ex.Message}", "Error");
            }
        }



        // ====================================================
        // PRIVATE HELPER METHODS
        // ====================================================

        
        /// Load asset data for editing, or initialize new asset
        
        private async Task LoadAsset()
        {
            try
            {
                Logger?.LogDebug("Loading asset data for AssessmentAssetId {AssessmentAssetId}", AssessmentAssetId);

                if (AssessmentAssetId > 0)
                {
                    // ✅ LOAD EXISTING ASSET
                    FormModel = await DataRepository.GetByIdAsync(AssessmentAssetId);

                    if (FormModel == null)
                    {
                        Logger?.LogWarning("Asset not found: {AssessmentAssetId}", AssessmentAssetId);
                        _Toast?.ShowError($"Asset not found", "Error");
                        FormModel = new AssessmentAsset { AssessmentId = AssessmentId };
                    }

                    Logger?.LogDebug(
                        "Loaded asset '{AssetName}': IsPreExisting={IsPreExisting}, AcquisitionStartMonth={AcquisitionStartMonth}",
                        FormModel.AssetName,
                        FormModel.IsPreExisting,
                        FormModel.AcquisitionStartMonth);
                }
                else
                {
                    // ✅ CREATE NEW ASSET
                    FormModel = new AssessmentAsset
                    {
                        AssessmentId = AssessmentId,
                        AcquisitionStartMonth = 0,  // Default: Pre-existing
                        OpeningBalanceValue = 0,
                        OpeningAccumulatedDepreciation = 0,
                        DepreciationRate = 10,  // Default 10% per year
                        DepreciationMethod = "Straight-Line",
                        IsDepreciable = true,
                        IsTangible = true,
                        Active = true
                    };

                    Logger?.LogDebug("Initialized new asset form for assessment {AssessmentId}", AssessmentId);
                }

                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                Logger?.LogError(ex, "Error loading asset data");
                throw;
            }
        }

        
        /// Get description for depreciation method
        
        public string GetDepreciationMethodDescription(string? method)
        {
            return method switch
            {
                "Straight-Line" => "Consistent depreciation each period",
                "Declining-Balance" => "Higher depreciation early on",
                "Units-of-Production" => "Depreciation based on usage or output",
                _ => "Select a depreciation method"
            };
        }


        // ====================================================
        // EVENT HANDLERS
        // ====================================================

        
        /// Handle inception type change (Pre-existing vs Acquired)
        
        private void OnInceptionTypeChanged(bool isPreExisting)
        {
            try
            {
                if (isPreExisting)
                {
                    FormModel.AcquisitionStartMonth = 0;
                }
                else
                {
                    FormModel.AcquisitionStartMonth = 1;
                    FormModel.OpeningAccumulatedDepreciation = 0;
                }

                StateHasChanged();
                Logger?.LogDebug("Asset inception type changed: IsPreExisting={IsPreExisting}", isPreExisting);
            }
            catch (Exception ex)
            {
                Logger?.LogError(ex, "Error in OnInceptionTypeChanged");
                _Toast?.ShowError($"Error: {ex.Message}", "Error");
            }
        }



        
        /// Save the asset form
        
        private async Task ExecuteSaveWorkflowAsync()
        {
            if (IsSubmitting)
                return;

            IsSubmitting = true;
            StateHasChanged();

            try
            {
                Logger?.LogInformation("Starting asset save workflow for assessment {AssessmentId}", AssessmentId);

                // ========== STEP 1: VALIDATION ==========
                if (!ValidateAssetForm())
                {
                    IsSubmitting = false;
                    StateHasChanged();
                    return;
                }

                // ========== STEP 2: PREPARE ASSET DATA ==========
                PrepareAssetData();

                Logger?.LogInformation(
                    "Saving asset '{AssetName}' for assessment {AssessmentId}. " +
                    "IsPreExisting={IsPreExisting}, AcquisitionStartMonth={AcquisitionStartMonth}, " +
                    "OpeningValue={OpeningValue}, DepreciationRate={DepRate}%",
                    FormModel.AssetName,
                    FormModel.AssessmentId,
                    FormModel.IsPreExisting,
                    FormModel.AcquisitionStartMonth,
                    FormModel.OpeningBalanceValue,
                    FormModel.DepreciationRate);

                // ========== STEP 3: SAVE ASSET MASTER RECORD ==========
                await DataRepository.SaveAsync(FormModel);

                Logger?.LogInformation(
                    "Asset '{AssetName}' saved successfully with ID {AssessmentAssetId}",
                    FormModel.AssetName,
                    FormModel.AssessmentAssetId);

                // ========== STEP 4: CREATE OR UPDATE MOVEMENT TEMPLATE ==========
                await CreateOrUpdateMovementTemplateAsync(FormModel);

                // ========== STEP 5: INVALIDATE PROJECTION CACHE ==========
                if (projectionStateManager != null)
                {
                    try
                    {
                        await projectionStateManager.InvalidateDataAsync(
                            "asset",
                            FormModel.AssessmentId,
                            FormModel.AssessmentAssetId);

                        Logger?.LogInformation(
                            "Projection cache invalidated for asset {AssessmentAssetId}",
                            FormModel.AssessmentAssetId);
                    }
                    catch (Exception ex)
                    {
                        Logger?.LogWarning(ex, "Error invalidating projection cache for asset {AssessmentAssetId}",
                            FormModel.AssessmentAssetId);
                    }
                }

                // ========== STEP 6: TRIGGER ASSESSMENT-LEVEL RECALCULATION ==========
                if (financialCalculationsEngine != null)
                {
                    try
                    {
                        await financialCalculationsEngine.RecalculateAssessmentTotalsAsync(FormModel.AssessmentId);

                        Logger?.LogInformation(
                            "Assessment totals recalculated after asset save for assessment {AssessmentId}",
                            FormModel.AssessmentId);
                    }
                    catch (Exception ex)
                    {
                        Logger?.LogWarning(ex,
                            "Error recalculating assessment totals after asset save. Asset was saved successfully.");
                        // Don't throw - asset was saved successfully, just recalculation failed
                    }
                }             

                // ========== STEP 7: CLOSE FORM ==========
                await Task.Delay(500);
                await zabCanvasService!.PublishResultAsync(
                    SaveResult.SavedAndClose($"Asset '{FormModel.AssetName}' saved successfully with movement template."));
            }
            catch (Exception ex)
            {
                Logger?.LogError(ex, "Error saving asset for assessment {AssessmentId}", FormModel.AssessmentId);               
                await zabCanvasService!.PublishResultAsync(
                    new SaveResult { Success = false, Message = $"Error: {ex.Message}" });
            }
            finally
            {
                IsSubmitting = false;
                StateHasChanged();
            }
        }

        
        /// Validates asset form data before saving
        /// Returns true if valid, false otherwise        
        private bool ValidateAssetForm()
        {
            try
            {
                Logger?.LogDebug("Validating asset form for asset '{AssetName}'", FormModel.AssetName);

                // Validate asset name
                if (string.IsNullOrWhiteSpace(FormModel.AssetName))
                {
                    _Toast?.ShowError("Asset name is required", "Validation Error");
                    Logger?.LogWarning("Validation failed: Asset name is empty");
                    return false;
                }

                // Validate opening value
                if (FormModel.OpeningBalanceValue <= 0)
                {
                    _Toast?.ShowError("Opening value must be greater than 0", "Validation Error");
                    Logger?.LogWarning("Validation failed: Opening balance value is {Value}", FormModel.OpeningBalanceValue);
                    return false;
                }

                // Validate acquisition month for mid-period acquired assets
                if (!FormModel.IsPreExisting)
                {
                    if (FormModel.AcquisitionStartMonth <= 0 || FormModel.AcquisitionStartMonth > 12)
                    {
                        _Toast?.ShowError("Acquisition month must be between 1 and 12", "Validation Error");
                        Logger?.LogWarning("Validation failed: Invalid acquisition month {Month}", FormModel.AcquisitionStartMonth);
                        return false;
                    }
                }

                // Validate depreciation rate if asset is depreciable
                if (FormModel.IsDepreciable)
                {
                    if (FormModel.DepreciationRate <= 0 || FormModel.DepreciationRate > 100)
                    {
                        _Toast?.ShowError("Depreciation rate must be between 0 and 100%", "Validation Error");
                        Logger?.LogWarning("Validation failed: Invalid depreciation rate {Rate}%", FormModel.DepreciationRate);
                        return false;
                    }

                    if (string.IsNullOrWhiteSpace(FormModel.DepreciationMethod))
                    {
                        _Toast?.ShowError("Depreciation method is required for depreciable assets", "Validation Error");
                        Logger?.LogWarning("Validation failed: Depreciation method is empty");
                        return false;
                    }
                }

                // Validate accumulated depreciation
                if (FormModel.OpeningAccumulatedDepreciation < 0)
                {
                    _Toast?.ShowError("Accumulated depreciation cannot be negative", "Validation Error");
                    Logger?.LogWarning("Validation failed: Accumulated depreciation is negative");
                    return false;
                }

                if (FormModel.OpeningAccumulatedDepreciation > FormModel.OpeningBalanceValue)
                {
                    _Toast?.ShowError("Accumulated depreciation cannot exceed opening value", "Validation Error");
                    Logger?.LogWarning(
                        "Validation failed: Accumulated depreciation ({AccumDepr}) exceeds opening value ({OpeningValue})",
                        FormModel.OpeningAccumulatedDepreciation,
                        FormModel.OpeningBalanceValue);
                    return false;
                }

                Logger?.LogDebug("Asset form validation passed");
                return true;
            }
            catch (Exception ex)
            {
                Logger?.LogError(ex, "Error during asset form validation");
                _Toast?.ShowError("Validation error occurred", "Error");
                return false;
            }
        }

        
        /// Prepares asset data for saving
        /// Sets default values and audit fields        
        private void PrepareAssetData()
        {
            try
            {
                Logger?.LogDebug("Preparing asset data for save");

                // ========== SET ASSESSMENT CONTEXT ==========
                FormModel.AssessmentId = AssessmentId;

                // ========== HANDLE PRE-EXISTING vs ACQUIRED LOGIC ==========
                if (!FormModel.IsPreExisting)
                {
                    // Mid-period acquired asset: Reset accumulated depreciation to 0
                    Logger?.LogDebug(
                        "Asset is acquired in month {Month}, resetting accumulated depreciation to 0",
                        FormModel.AcquisitionStartMonth);

                    FormModel.OpeningAccumulatedDepreciation = 0;
                }
                else
                {
                    // Pre-existing asset: Keep accumulated depreciation as entered
                    Logger?.LogDebug(
                        "Asset is pre-existing with accumulated depreciation of {AccumDepr}",
                        FormModel.OpeningAccumulatedDepreciation);
                }

                // ========== SET DEPRECIATION DEFAULTS ==========
                if (!FormModel.IsDepreciable)
                {
                    // Non-depreciable asset
                    FormModel.DepreciationRate = 0;
                    FormModel.DepreciationMethod = null;
                    Logger?.LogDebug("Asset is non-depreciable, clearing depreciation fields");
                }
                else
                {
                    // Depreciable asset: Ensure method is set
                    if (string.IsNullOrWhiteSpace(FormModel.DepreciationMethod))
                    {
                        FormModel.DepreciationMethod = "Straight-Line"; // Default
                        Logger?.LogDebug("Setting default depreciation method to Straight-Line");
                    }
                }

                // ========== SET AUDIT FIELDS ==========
                if (FormModel.AssessmentAssetId == 0)
                {
                    // New record
                    FormModel.CreatedDate = DateTime.UtcNow;
                    FormModel.CreatedBy = sessionService?.UserId ?? 0;
                    Logger?.LogDebug("New asset: Setting CreatedDate and CreatedBy");
                }
                else
                {
                    // Existing record
                    FormModel.ModifiedDate = DateTime.UtcNow;
                    FormModel.ModifiedBy = sessionService?.UserId ?? 0;
                    Logger?.LogDebug("Existing asset: Updating ModifiedDate and ModifiedBy");
                }

                // ========== ENSURE RECORD IS ACTIVE ==========
                FormModel.Active = true;

                Logger?.LogDebug("Asset data preparation complete");
            }
            catch (Exception ex)
            {
                Logger?.LogError(ex, "Error preparing asset data");
                throw;
            }
        }



        /// Creates a new movement template or updates existing one for an asset
        /// Ensures every asset has a corresponding movement record for 12-month tracking
        /// 
        /// Process:
        /// 1. Check if movement record already exists
        /// 2. If exists: Update with latest asset data
        /// 3. If not exists: Create new one and initialize defaults
        /// 4. Save to database

        /// <summary>
        /// Creates a new movement template or updates existing one for an asset
        /// ✅ GUARANTEES DEPRECIATION RATE IS COPIED AT SAVE TIME
        /// </summary>
        private async Task CreateOrUpdateMovementTemplateAsync(AssessmentAsset asset)
        {
            try
            {
                Logger?.LogInformation(
                    "Creating or updating movement template for asset {AssetId} (Name: {AssetName}). " +
                    "DepreciationRate: {DepRate}%, DepreciationMethod: {Method}",
                    asset.AssessmentAssetId,
                    asset.AssetName,
                    asset.DepreciationRate,
                    asset.DepreciationMethod);

                // ========== STEP 1: CHECK IF MOVEMENT RECORD EXISTS ==========
                var existingMovements = await MovementRepository.GetAllAsync(m =>
                    m.AssessmentAssetId == asset.AssessmentAssetId && m.Active);

                AssessmentAssetMovement movementRecord;

                if (existingMovements != null && existingMovements.Count() > 0)
                {
                    // ✅ CASE 1: Movement record exists → Update it
                    movementRecord = existingMovements.FirstOrDefault()!;

                    Logger?.LogDebug(
                        "Movement record found for asset {AssetId}. " +
                        "Old DepreciationRate: {OldRate}%, New DepreciationRate: {NewRate}%",
                        asset.AssessmentAssetId,
                        movementRecord.DepreciationRate,
                        asset.DepreciationRate);

                    // Update audit fields
                    movementRecord.ModifiedDate = DateTime.UtcNow;
                    movementRecord.ModifiedBy = sessionService?.UserId ?? 0;
                    movementRecord.Active = true;

                    // ✅ COPY CURRENT DEPRECIATION DATA FROM ASSET MASTER
                    movementRecord.DepreciationRate = asset.DepreciationRate;
                    movementRecord.DepreciationMethod = asset.DepreciationMethod;

                    Logger?.LogDebug(
                        "Depreciation data updated in movement record. " +
                        "DepreciationRate: {Rate}%, DepreciationMethod: {Method}",
                        movementRecord.DepreciationRate,
                        movementRecord.DepreciationMethod);

                    // Recalculate all months based on updated asset data and depreciation rate
                    Logger?.LogDebug("Recalculating all months with updated depreciation data");

                    if (movementCalculationService != null)
                    {
                        movementRecord = await movementCalculationService.RecalculateAllMonthsAsync(
                            movementRecord,
                            asset);
                    }
                }
                else
                {
                    // ✅ CASE 2: Movement record doesn't exist → Create new one
                    Logger?.LogDebug(
                        "No movement record found for asset {AssetId}. " +
                        "Creating new template with DepreciationRate: {Rate}%, Method: {Method}",
                        asset.AssessmentAssetId,
                        asset.DepreciationRate,
                        asset.DepreciationMethod);

                    if (movementCalculationService != null)
                    {
                        // Use service to initialize with proper calculations
                        // ✅ Service will capture depreciation rate in InitializeMovementTemplateAsync
                        movementRecord = await movementCalculationService.InitializeMovementTemplateAsync(asset);

                        // Set audit fields for new record
                        movementRecord.CreatedDate = DateTime.UtcNow;
                        movementRecord.CreatedBy = sessionService?.UserId ?? 0;

                        Logger?.LogDebug(
                            "New movement template created with DepreciationRate: {Rate}%, Method: {Method}",
                            movementRecord.DepreciationRate,
                            movementRecord.DepreciationMethod);
                    }
                    else
                    {
                        // Fallback if service is not available
                        Logger?.LogWarning("MovementCalculationService not available, creating basic template");

                        movementRecord = new AssessmentAssetMovement
                        {
                            AssessmentAssetId = asset.AssessmentAssetId,
                            AssessmentId = asset.AssessmentId,
                            CreatedDate = DateTime.UtcNow,
                            CreatedBy = sessionService?.UserId ?? 0,
                            Active = true,

                            // ✅ EVEN IN FALLBACK, COPY DEPRECIATION DATA
                            DepreciationRate = asset.DepreciationRate,
                            DepreciationMethod = asset.DepreciationMethod
                        };

                        Logger?.LogDebug(
                            "Fallback template created with DepreciationRate: {Rate}%, Method: {Method}",
                            movementRecord.DepreciationRate,
                            movementRecord.DepreciationMethod);
                    }
                }

                // ========== STEP 2: VALIDATION - ENSURE DEPRECIATION RATE IS SET ==========
                if (asset.IsDepreciable && movementRecord.DepreciationRate != asset.DepreciationRate)
                {
                    Logger?.LogError(
                        "DEPRECIATION RATE MISMATCH! Asset: {AssetRate}%, Movement: {MovementRate}%",
                        asset.DepreciationRate,
                        movementRecord.DepreciationRate);

                    // Force update to ensure they match
                    movementRecord.DepreciationRate = asset.DepreciationRate;
                    movementRecord.DepreciationMethod = asset.DepreciationMethod;

                    Logger?.LogWarning("Corrected depreciation rate mismatch in movement record");
                }

                // ========== STEP 3: SAVE THE MOVEMENT RECORD ==========
                await MovementRepository.SaveAsync(movementRecord);

                Logger?.LogInformation(
                    "Movement template saved successfully for asset {AssetId}. " +
                    "Template ID: {TemplateId}, DepreciationRate: {Rate}%, Method: {Method}",
                    asset.AssessmentAssetId,
                    movementRecord.AssessmentAssetMovementId,
                    movementRecord.DepreciationRate,
                    movementRecord.DepreciationMethod);
            }
            catch (Exception ex)
            {
                Logger?.LogError(ex,
                    "Error creating/updating movement template for asset {AssetId}",
                    asset.AssessmentAssetId);

                _Toast?.ShowWarning(
                    "Movement template could not be saved, but asset was saved successfully",
                    "Warning");

                Logger?.LogWarning(
                    "Movement template creation failed, but asset save succeeded. " +
                    "Movement template may need to be regenerated manually.");
            }
        }


        /// Cancel form and close offcanvas

        private async Task CancelFormAsync()
        {
            try
            {
                Logger?.LogDebug("User cancelled asset form");
                await zabCanvasService!.HideAsync(SaveResult.Cancel());
            }
            catch (Exception ex)
            {
                Logger?.LogError(ex, "Error cancelling form");
            }
        }

    }
}
