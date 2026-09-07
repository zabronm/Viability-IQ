using Microsoft.AspNetCore.Components;
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
    
    /// AssetCategoryFormComponent - Form for adding and editing asset categories
    /// Categories organize assets on the balance sheet and determine default settings    
    public partial class AssetCategoryFormComponent : ComponentBase
    {
       
        /// The AssetCategory ID to edit. If 0, creates a new category        
        [Parameter]        public long AssetCategoryId { get; set; }        
        [Inject]        private IGenericDataRepository<AssetCategory> categoryRepository { get; set; } = default!;
        [Inject]        private ISessionService? sessionService { get; set; }
        [Inject]        private ToastService? _Toast { get; set; }
       
        private AssetCategory category = new();
        private List<string> balanceSheetSections = new();
        
        private Dictionary<string, string> validationErrors = new();
        private bool isLoading = false;
        private bool isSaving = false;
        private string successMessage = string.Empty;      
        
        /// Initialize component - load category if editing, populate balance sheet sections        
        protected override async Task OnInitializedAsync()
        {
            isLoading = true;

            try
            {
                // ✅ Load balance sheet sections
                PopulateBalanceSheetSections();

                // ✅ Load existing category if editing
                if (AssetCategoryId > 0)
                {
                    var existingCategory = await categoryRepository.GetByIdAsync(AssetCategoryId);
                    if (existingCategory != null)
                    {
                        category = existingCategory;
                    }
                    else
                    {
                        _Toast?.ShowError($"Category with ID {AssetCategoryId} not found.", "Error");
                        return;
                    }
                }
                else
                {
                    // ✅ Create new category with defaults
                    category = new AssetCategory
                    {
                        CreatedDate = DateTime.UtcNow,
                        CreatedBy = sessionService?.UserId ?? 0,
                        Active = true,
                        IsCurrentAsset = false,
                        DisplayOrder = 0
                    };
                }
            }
            catch (Exception ex)
            {
                _Toast?.ShowError($"Failed to load form data: {ex.Message}", "Error");
                System.Diagnostics.Debug.WriteLine($"[AssetCategoryFormComponent] Error in OnInitializedAsync: {ex}");
            }
            finally
            {
                isLoading = false;
            }
        }

        // ====================================================
        // PUBLIC EVENT HANDLERS - Invoked from Razor Markup
        // ====================================================

        
        /// Handle classification (Current/Fixed asset) change
        /// Updates balance sheet section options based on selection
        
        public void OnClassificationChanged(ChangeEventArgs e)
        {
            category.IsCurrentAsset = (bool)e.Value!;
            // Reset balance sheet section when classification changes
            category.BalanceSheetSection = null;
            StateHasChanged();
        }

        
        /// Handle form submission
        /// Validates data and saves category to database
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
                if (category.AssetCategoryId > 0)
                {
                    // UPDATE
                    category.ModifiedDate = DateTime.UtcNow;
                    category.ModifiedBy = sessionService?.UserId ?? 0;
                }
                else
                {
                    // CREATE
                    category.CreatedDate = DateTime.UtcNow;
                    category.CreatedBy = sessionService?.UserId ?? 0;
                    category.Active = true;
                }

                // ✅ STEP 3: SAVE CATEGORY
                var success = await categoryRepository.SaveAsync(category);

                if (success)
                {
                    // ✅ STEP 4: SHOW SUCCESS MESSAGE
                    successMessage = category.AssetCategoryId == 0
                        ? "Category added successfully!"
                        : "Category updated successfully!";

                    _Toast?.ShowSuccess(successMessage, sessionService?.AppTitle ?? "Success");

                    System.Diagnostics.Debug.WriteLine($"[AssetCategoryFormComponent] Category saved successfully. ID: {category.AssetCategoryId}, Name: {category.CategoryName}");

                    // ✅ STEP 5: RETURN SUCCESS TO CALLER
                    await Task.Delay(500);  // Brief delay to show success message
                    ReturnSuccess($"Category '{category.CategoryName}' saved successfully.");
                }
                else
                {
                    _Toast?.ShowError("Failed to save category. Please try again.", "Error");
                }
            }
            catch (Exception ex)
            {
                _Toast?.ShowError($"Error saving category: {ex.Message}", "Error");
                System.Diagnostics.Debug.WriteLine($"[AssetCategoryFormComponent] Exception in HandleSubmit: {ex}");
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
        
        public void HandleCancel()
        {
            ReturnCancel();
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
        // PRIVATE HELPER METHODS
        // ====================================================

        
        /// Populate balance sheet section options
        
        private void PopulateBalanceSheetSections()
        {
            balanceSheetSections = new List<string>
            {
                "Current Assets",
                "Fixed Assets",
                "Land and Buildings",
                "Plant and Machinery",
                "Furniture and Fittings",
                "Motor Vehicles",
                "Intangible Assets",
                "Investments",
                "Goodwill",
                "Other Assets"
            };
        }

        // ====================================================
        // PRIVATE VALIDATION METHODS
        // ====================================================

        
        /// Validate all form fields
        
        private bool ValidateForm()
        {
            bool isValid = true;

            // ✅ Category Name validation
            if (string.IsNullOrWhiteSpace(category.CategoryName))
            {
                validationErrors[nameof(category.CategoryName)] = "Category Name is required";
                isValid = false;
            }
            else if (category.CategoryName.Length > 100)
            {
                validationErrors[nameof(category.CategoryName)] = "Category Name cannot exceed 100 characters";
                isValid = false;
            }

            // ✅ Balance Sheet Section validation
            if (string.IsNullOrWhiteSpace(category.BalanceSheetSection))
            {
                validationErrors[nameof(category.BalanceSheetSection)] = "Balance Sheet Section is required";
                isValid = false;
            }

            // ✅ Display Order validation
            if (category.DisplayOrder < 0)
            {
                validationErrors[nameof(category.DisplayOrder)] = "Display Order must be 0 or greater";
                isValid = false;
            }

            return isValid;
        }

        // ====================================================
        // PRIVATE CALLBACK METHODS
        // ====================================================

        
        /// Return success result to the OffCanvasService
        
        private void ReturnSuccess(string message)
        {
            try
            {
                // Create success result
                var result = new SaveResult
                {
                    Success = true,
                    Message = message,
                    Data = category
                };

                System.Diagnostics.Debug.WriteLine($"[AssetCategoryFormComponent] ReturnSuccess called. Message: {message}");

                // In production, this would be dispatched through OffCanvasService
                Task.Run(async () =>
                {
                    await Task.CompletedTask;
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AssetCategoryFormComponent] Error in ReturnSuccess: {ex}");
                _Toast?.ShowError($"Error returning result: {ex.Message}", "Error");
            }
        }

        
        /// Return cancel result to the OffCanvasService
        
        private void ReturnCancel()
        {
            try
            {
                // Create cancel result
                var result = new SaveResult
                {
                    Success = false,
                    Message = "Operation cancelled by user",
                    Data = null
                };

                System.Diagnostics.Debug.WriteLine("[AssetCategoryFormComponent] ReturnCancel called - Operation cancelled");

                // In production, this would be dispatched through OffCanvasService
                Task.Run(async () =>
                {
                    await Task.CompletedTask;
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AssetCategoryFormComponent] Error in ReturnCancel: {ex}");
                _Toast?.ShowError($"Error cancelling: {ex.Message}", "Error");
            }
        }
    }
}

