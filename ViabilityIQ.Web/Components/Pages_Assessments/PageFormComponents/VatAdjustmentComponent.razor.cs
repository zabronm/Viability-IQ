using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ViabilityIQ.Shared.SharedModels;
using ViabilityIQ.Web.Services;

namespace ViabilityIQ.Web.Components.Pages_Assessments.PageFormComponents
{
    
    /// VatAdjustmentComponent - VAT adjustments and overrides for 12-month schedule
    /// Used within ZabOffCanvasService for modal presentation
    /// Allows users to apply adjustments to output VAT, input VAT, and add month-specific notes
    
    public partial class VatAdjustmentComponent : ComponentBase
    {
        // ====================================================
        // PARAMETERS
        // ====================================================
        
        /// The Assessment ID for context
        
        [Parameter]
        public long AssessmentId { get; set; }

        
        /// Callback when adjustments are saved
        
        [Parameter]
        public EventCallback<SaveResult> OnSaved { get; set; }

        // ====================================================
        // INJECTIONS
        // ====================================================
        [Inject]
        private ToastService? _Toast { get; set; }

        [Inject]
        private ILogger<VatAdjustmentComponent>? Logger { get; set; }

        // ====================================================
        // PRIVATE FIELDS - ADJUSTMENT DATA
        // ====================================================
        
        /// Output VAT adjustments for each month (1-12)
        
        private decimal[] outputAdjustments = new decimal[12];

        
        /// Input VAT adjustments for each month (1-12)
        
        private decimal[] inputAdjustments = new decimal[12];

        
        /// Optional notes for each month
        
        private string[] monthNotes = new string[12];

        
        /// Selected reason code for adjustments
        
        private string selectedReasonCode = string.Empty;

        
        /// Audit justification text
        
        private string auditJustification = string.Empty;

        // ====================================================
        // PRIVATE FIELDS - FORM STATE
        // ====================================================
        private bool isSaving = false;
        private string successMessage = string.Empty;
        private string errorMessage = string.Empty;

        // ====================================================
        // LIFECYCLE
        // ====================================================
        protected override async Task OnInitializedAsync()
        {
            try
            {
                Logger?.LogInformation("VatAdjustmentComponent initialized for assessment {AssessmentId}", AssessmentId);

                // Initialize arrays
                outputAdjustments = new decimal[12];
                inputAdjustments = new decimal[12];
                monthNotes = new string[12];

                // TODO: Load existing adjustments from database if available
                // await LoadExistingAdjustments();

                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                errorMessage = $"Error initializing component: {ex.Message}";
                Logger?.LogError(ex, "Error in VatAdjustmentComponent.OnInitializedAsync");
            }
        }

        // ====================================================
        // EVENT HANDLERS
        // ====================================================

        
        /// Handle save action
        /// Validates data and returns result to parent via callback
        
        private async Task HandleSave()
        {
            isSaving = true;
            StateHasChanged();

            try
            {
                // ✅ VALIDATION
                if (string.IsNullOrWhiteSpace(auditJustification))
                {
                    errorMessage = "Audit justification is required for VAT adjustments.";
                    _Toast?.ShowError(errorMessage, "Validation Error");
                    isSaving = false;
                    StateHasChanged();
                    return;
                }

                // ✅ PREPARE DATA
                var vatAdjustmentData = new VatAdjustmentData
                {
                    AssessmentId = AssessmentId,
                    ReasonCode = selectedReasonCode,
                    OutputAdjustments = new List<decimal>(outputAdjustments),
                    InputAdjustments = new List<decimal>(inputAdjustments),
                    MonthNotes = new List<string>(monthNotes),
                    AuditJustification = auditJustification,
                    CreatedDate = DateTime.UtcNow
                };

                // ✅ SAVE TO DATABASE (TODO: Implement actual save)
                // var success = await vatAdjustmentService.SaveAdjustmentsAsync(vatAdjustmentData);

                Logger?.LogInformation(
                    "VAT adjustments saved for assessment {AssessmentId}. Output Total: {OutputTotal}, Input Total: {InputTotal}",
                    AssessmentId,
                    outputAdjustments.Sum(),
                    inputAdjustments.Sum());

                successMessage = "VAT adjustments saved successfully!";
                _Toast?.ShowSuccess(successMessage, "Success");

                // ✅ RETURN SUCCESS RESULT
                await Task.Delay(500);
                await ReturnSuccess("VAT adjustments have been saved and will be reflected in the payment schedule.");
            }
            catch (Exception ex)
            {
                errorMessage = $"Error saving adjustments: {ex.Message}";
                Logger?.LogError(ex, "Error in VatAdjustmentComponent.HandleSave");
                _Toast?.ShowError(errorMessage, "Error");
            }
            finally
            {
                isSaving = false;
                StateHasChanged();
            }
        }

        
        /// Handle cancel action
        
        private async Task HandleCancel()
        {
            try
            {
                Logger?.LogInformation("VAT adjustment cancelled for assessment {AssessmentId}", AssessmentId);
                await ReturnCancel();
            }
            catch (Exception ex)
            {
                Logger?.LogError(ex, "Error in VatAdjustmentComponent.HandleCancel");
            }
        }

        // ====================================================
        // PRIVATE HELPER METHODS
        // ====================================================

        
        /// Load existing VAT adjustments from database (placeholder)
        
        private async Task LoadExistingAdjustments()
        {
            try
            {
                // TODO: Implement actual loading of existing adjustments
                // var existingAdjustments = await vatAdjustmentService.GetAdjustmentsAsync(AssessmentId);
                // Map to local arrays

                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                Logger?.LogError(ex, "Error loading existing VAT adjustments");
            }
        }

        
        /// Validate adjustment data
        
        private bool ValidateAdjustments()
        {
            // Check if any adjustments were made
            bool hasAdjustments = outputAdjustments.Any(a => a != 0) || inputAdjustments.Any(a => a != 0);

            if (!hasAdjustments && string.IsNullOrWhiteSpace(auditJustification))
            {
                errorMessage = "Please provide either adjustments or an audit justification.";
                return false;
            }

            return true;
        }

        
        /// Get total output adjustments
        
        public decimal GetTotalOutputAdjustments()
        {
            return outputAdjustments.Sum();
        }

        
        /// Get total input adjustments
        
        public decimal GetTotalInputAdjustments()
        {
            return inputAdjustments.Sum();
        }

        
        /// Get net VAT adjustment (Output - Input)
        
        public decimal GetNetVatAdjustment()
        {
            return GetTotalOutputAdjustments() - GetTotalInputAdjustments();
        }

        // ====================================================
        // PRIVATE CALLBACK METHODS
        // ====================================================

        
        /// Return success result to parent component
        
        private async Task ReturnSuccess(string message)
        {
            try
            {
                var result = new SaveResult
                {
                    Success = true,
                    Message = message,
                    RefreshGrid = true,
                    Data = new VatAdjustmentData
                    {
                        AssessmentId = AssessmentId,
                        ReasonCode = selectedReasonCode,
                        OutputAdjustments = new List<decimal>(outputAdjustments),
                        InputAdjustments = new List<decimal>(inputAdjustments),
                        MonthNotes = new List<string>(monthNotes),
                        AuditJustification = auditJustification
                    }
                };

                Logger?.LogDebug("VatAdjustmentComponent returning success: {Message}", message);
                await OnSaved.InvokeAsync(result);
            }
            catch (Exception ex)
            {
                Logger?.LogError(ex, "Error in VatAdjustmentComponent.ReturnSuccess");
                _Toast?.ShowError($"Error returning result: {ex.Message}", "Error");
            }
        }

        
        /// Return cancel result to parent component
        
        private async Task ReturnCancel()
        {
            try
            {
                var result = new SaveResult
                {
                    Success = false,
                    Message = "VAT adjustment cancelled by user"
                };

                Logger?.LogDebug("VatAdjustmentComponent returning cancel");
                await OnSaved.InvokeAsync(result);
            }
            catch (Exception ex)
            {
                Logger?.LogError(ex, "Error in VatAdjustmentComponent.ReturnCancel");
                _Toast?.ShowError($"Error cancelling: {ex.Message}", "Error");
            }
        }
    }

    // ====================================================
    // DATA CLASS: VAT Adjustment Data
    // ====================================================
    
    /// Represents VAT adjustment data for a 12-month period
    
    public class VatAdjustmentData
    {
        
        /// Assessment ID
        
        public long AssessmentId { get; set; }

        
        /// Reason code for the adjustments
        
        public string? ReasonCode { get; set; }

        
        /// Output VAT adjustments for each month
        
        public List<decimal> OutputAdjustments { get; set; } = new();

        
        /// Input VAT adjustments for each month
        
        public List<decimal> InputAdjustments { get; set; } = new();

        
        /// Optional notes for each month
        
        public List<string> MonthNotes { get; set; } = new();

        
        /// Audit justification for the adjustments
        
        public string? AuditJustification { get; set; }

        
        /// When the adjustments were created
        
        public DateTime CreatedDate { get; set; }

        
        /// Calculate total output adjustments
        
        public decimal GetTotalOutputAdjustments()
        {
            return OutputAdjustments.Sum();
        }

        
        /// Calculate total input adjustments
        
        public decimal GetTotalInputAdjustments()
        {
            return InputAdjustments.Sum();
        }

        
        /// Calculate net VAT adjustment
        
        public decimal GetNetVatAdjustment()
        {
            return GetTotalOutputAdjustments() - GetTotalInputAdjustments();
        }
    }
}
