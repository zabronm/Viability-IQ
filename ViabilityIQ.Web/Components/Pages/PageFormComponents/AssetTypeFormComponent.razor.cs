using Microsoft.AspNetCore.Components;
using Serilog.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ViabilityIQ.Application.Interfaces;
using ViabilityIQ.Shared.DataModels;
using ViabilityIQ.Shared.SharedModels;
using ViabilityIQ.Web.Services;


namespace ViabilityIQ.Web.Components.Pages.PageFormComponents
{
    /// AssetTypeFormComponent - Form for adding and editing asset types
    /// Asset types are specific classifications within categories with default depreciation settings

    public partial class AssetTypeFormComponent : ComponentBase
    {

        [Parameter] public long AssetTypeId { get; set; }

        // ====================================================
        // INJECTIONS

        [Inject] private IGenericDataRepository<AssetType> typeRepository { get; set; } = default!;
        [Inject] private IGenericDataRepository<AssetCategory> categoryRepository { get; set; } = default!;
        [Inject] private ISessionService? sessionService { get; set; }
        [Inject] private ToastService? _Toast { get; set; }
        [Inject] private OffCanvasStateService OffcanvasService { get; set; } = default!;
        [Inject] private ILogger<BusinessFormComponent> Logger { get; set; } = default!;

        // ====================================================
        // PRIVATE FIELDS - FORM DATA
        // ====================================================
        private AssetType type = new();
        private List<AssetCategory> categoriesList = new();

        // ====================================================
        // PRIVATE FIELDS - FORM STATE
        // ====================================================
        private Dictionary<string, string> validationErrors = new();
        private bool isLoading = false;
        private bool isSaving = false;
        private string successMessage = string.Empty;
        private bool IsDepreciable
        {
            get => type.IsDepreciable;
            set
            {
                type.IsDepreciable = value;
                if (!value)
                {
                    type.DefaultDepreciationRate = 0M;
                    type.DefaultUsefulLifeYears = null;
                }
            }
        }

        // ====================================================
        // LIFECYCLE METHODS
        // ====================================================


        /// Initialize component - load categories and existing type if editing

        protected override async Task OnInitializedAsync()
        {
            isLoading = true;

            try
            {
                // ✅ Load all categories
                var categories = await categoryRepository.GetAllAsync();
                categoriesList = categories.ToList();

                // ✅ Load existing type if editing
                if (AssetTypeId > 0)
                {
                    var existingType = await typeRepository.GetByIdAsync(AssetTypeId);
                    if (existingType != null)
                    {
                        type = existingType;
                    }
                    else
                    {
                        _Toast?.ShowError($"Asset type with ID {AssetTypeId} not found.", "Error");
                        return;
                    }
                }
                else
                {
                    // ✅ Create new type with defaults
                    type = new AssetType
                    {
                        CreatedDate = DateTime.UtcNow,
                        CreatedBy = sessionService?.UserId ?? 0,
                        Active = true,
                        IsDepreciable = true,
                        IsCurrent = false,
                        DefaultDepreciationRate = 10M,
                        DefaultUsefulLifeYears = 10
                    };
                }
            }
            catch (Exception ex)
            {
                _Toast?.ShowError($"Failed to load form data: {ex.Message}", "Error");
                System.Diagnostics.Debug.WriteLine($"[AssetTypeFormComponent] Error in OnInitializedAsync: {ex}");
            }
            finally
            {
                isLoading = false;
            }
        }

        // ====================================================
        // PUBLIC EVENT HANDLERS - Invoked from Razor Markup
        // ====================================================


        /// Handle depreciable checkbox change
        /// Shows/hides depreciation settings

        public void OnDepreciableChanged(ChangeEventArgs e)
        {
            type.IsDepreciable = (bool)e.Value!;
            if (!type.IsDepreciable)
            {
                // Reset depreciation settings if not depreciable
                type.DefaultDepreciationRate = 0M;
                type.DefaultUsefulLifeYears = null;
            }
            StateHasChanged();
        }


        /// Handle current asset checkbox change

        public void OnAssetClassChanged(ChangeEventArgs e)
        {
            type.IsCurrent = (bool)e.Value!;
            StateHasChanged();
        }


        /// Handle form submission
        /// Validates data and saves type to database
        /// PUBLIC - invoked from form @onsubmit event

        public async Task HandleSubmit()
        {
            validationErrors.Clear();

            // ✅ STEP 1: VALIDATION
            if (!ValidateForm())
            {
                return;
            }

            isSaving = true;
            StateHasChanged();

            try
            {
                // ✅ STEP 2: SET AUDIT TRAIL
                if (type.AssetTypeId > 0)
                {
                    // UPDATE
                    type.ModifiedDate = DateTime.UtcNow;
                    type.ModifiedBy = sessionService?.UserId ?? 0;
                }
                else
                {
                    // CREATE
                    type.CreatedDate = DateTime.UtcNow;
                    type.CreatedBy = sessionService?.UserId ?? 0;
                    type.Active = true;
                }

                // ✅ STEP 3: SAVE TYPE
                var success = await typeRepository.SaveAsync(type);

                if (success)
                {
                    // ✅ STEP 4: SHOW SUCCESS MESSAGE
                    //successMessage = type.AssetTypeId == 0
                    //    ? "Asset type added successfully!"
                    //    : "Asset type updated successfully!";

                    //_Toast?.ShowSuccess(successMessage, sessionService?.AppTitle ?? "Success");
                    var saveResult = new SaveResult()
                    {
                        Success = true,
                        RefreshGrid = true,
                        ClosePanel = true,  // ✅ Always close on success
                        Message = AssetTypeId == 0
                            ? $"{type.TypeName} added successfully"
                            : $"{type.TypeName} updated successfully"
                    };

                    System.Diagnostics.Debug.WriteLine($"[AssetTypeFormComponent] Type saved successfully. ID: {type.AssetTypeId}, Name: {type.TypeName}");

                    // ✅ STEP 5: RETURN SUCCESS TO CALLER
                    await ReturnSuccessAsync($"Asset type '{type.TypeName}' saved successfully.");
                }
                else
                {
                    var saveResult = new SaveResult()
                    {
                        Success = false,
                        RefreshGrid = false,
                        ClosePanel = false,
                        Message = "Asset Type not saved/updated"
                    };
                    await ReturnSuccessAsync($"Asset type '{type.TypeName}' not saved/updated.");
                }
            }
            catch (Exception ex)
            {
                await ReturnSuccessAsync($"Error saving/updating Asset type '{type.TypeName}'; not saved/updated.");
                Logger.LogError(ex, $"Business save failed for Asset Type {type.TypeName}");
                System.Diagnostics.Debug.WriteLine($"[AssetTypeFormComponent] Exception in HandleSubmit: {ex}");

            }
            finally
            {
                isSaving = false;
                StateHasChanged();
            }
        }


        /// Handle form cancellation
        /// Closes the form without saving
        /// PUBLIC - invoked from button @onclick event

        public async Task HandleCancel()
        {
            await ReturnCancelAsync();
        }

        // ====================================================
        // PUBLIC VALIDATION HELPERS - Invoked from Razor Markup
        // ====================================================


        /// Check if a field has a validation error
        /// PUBLIC - invoked from Razor markup conditional rendering

        public bool HasError(string fieldName)
        {
            return validationErrors.ContainsKey(fieldName);
        }


        /// Get the validation error message for a field
        /// PUBLIC - invoked from Razor markup to display error text

        public string GetError(string fieldName)
        {
            return validationErrors.ContainsKey(fieldName) ? validationErrors[fieldName] : string.Empty;
        }

        // ====================================================
        // PUBLIC UI HELPER METHODS - Invoked from Razor Markup
        // ====================================================


        /// Get category name for display in preview

        public string GetCategoryName()
        {
            var category = categoriesList.FirstOrDefault(c => c.AssetCategoryId == type.AssetCategoryId);
            return category?.CategoryName ?? "Unknown";
        }

        // ====================================================
        // PRIVATE VALIDATION METHODS
        // ====================================================


        /// Validate all form fields

        private bool ValidateForm()
        {
            bool isValid = true;

            // ✅ Type Name validation
            if (string.IsNullOrWhiteSpace(type.TypeName))
            {
                validationErrors[nameof(type.TypeName)] = "Type Name is required";
                isValid = false;
            }
            else if (type.TypeName.Length > 100)
            {
                validationErrors[nameof(type.TypeName)] = "Type Name cannot exceed 100 characters";
                isValid = false;
            }

            // ✅ Category validation
            if (type.AssetCategoryId <= 0)
            {
                validationErrors[nameof(type.AssetCategoryId)] = "Category is required";
                isValid = false;
            }

            // ✅ Depreciation Rate validation (if depreciable)
            if (type.IsDepreciable)
            {
                if (type.DefaultDepreciationRate < 0)
                {
                    validationErrors[nameof(type.DefaultDepreciationRate)] = "Depreciation Rate cannot be negative";
                    isValid = false;
                }
                else if (type.DefaultDepreciationRate > 100)
                {
                    validationErrors[nameof(type.DefaultDepreciationRate)] = "Depreciation Rate cannot exceed 100%";
                    isValid = false;
                }

                // ✅ Useful Life validation (if depreciable)
                if (!type.DefaultUsefulLifeYears.HasValue || type.DefaultUsefulLifeYears <= 0)
                {
                    validationErrors[nameof(type.DefaultUsefulLifeYears)] = "Useful Life must be greater than 0 for depreciable assets";
                    isValid = false;
                }
            }

            return isValid;
        }

        // ====================================================
        // PRIVATE CALLBACK METHODS
        // ====================================================


        /// Return success result to the OffCanvasService

        private async Task ReturnSuccessAsync(string message)
        {
            try
            {
                var result = new SaveResult
                {
                    Success = true,
                    ClosePanel = true,
                    RefreshGrid = true,
                    Message = message,
                    Data = type
                };

                System.Diagnostics.Debug.WriteLine($"[AssetTypeFormComponent] ReturnSuccess called. Message: {message}");
                await OffcanvasService.PublishResultAsync(result);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AssetTypeFormComponent] Error in ReturnSuccess: {ex}");
                _Toast?.ShowError($"Error returning result: {ex.Message}", "Error");
            }
        }


        /// Return cancel result to the OffCanvasService

        private async Task ReturnCancelAsync()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("[AssetTypeFormComponent] ReturnCancel called - Operation cancelled");
                await OffcanvasService.PublishResultAsync(SaveResult.Cancel());
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AssetTypeFormComponent] Error in ReturnCancel: {ex}");
                _Toast?.ShowError($"Error cancelling: {ex.Message}", "Error");
            }
        }
    }
}
