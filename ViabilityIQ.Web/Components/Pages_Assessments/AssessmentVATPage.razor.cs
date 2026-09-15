using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ViabilityIQ.Application.Interfaces;
using ViabilityIQ.Infrastructure.Repositories;
using ViabilityIQ.Shared.SharedModels;
using ViabilityIQ.Web.Components.CommonComponents;
using ViabilityIQ.Web.Components.Pages_Assessments.PageFormComponents;
using ViabilityIQ.Web.Services;

namespace ViabilityIQ.Web.Components.Pages_Assessments
{
    
    /// AssessmentVATPage - VAT Projections & Schedule Management
    /// Displays 12-month VAT calculations with adjustment capabilities
    /// Uses ZabOffCanvasService for modal interactions
    
    public partial class AssessmentVATPage: IAsyncDisposable
    {
        // ====================================================
        // INJECTIONS
        // ====================================================
        [Inject]
        private MasterDataService? ViqCrudService { get; set; }

        [Inject]
        private ISessionService? sessionService { get; set; }

        [Inject]
        private ZabOffCanvasService? zabCanvasService { get; set; }

        [Inject]
        private ToastService? _Toast { get; set; }

        [Inject]
        private IProjectionStateManager? projectionStateManager { get; set; }

        [Inject]
        private ILogger<AssessmentSalesPage>? Logger { get; set; }

        // ====================================================
        // PARAMETERS
        // ====================================================
        [Parameter]
        public long AssessmentId { get; set; } = 1;

        [Parameter]
        public EventCallback<SaveResult> OnSaveComplete { get; set; }

        // ====================================================
        // PRIVATE FIELDS - DATA
        // ====================================================
        // Sample Data Arrays for 12 Months
        private decimal[] Sales = { 150000, 165000, 180000, 170000, 190000, 200000, 210000, 195000, 215000, 220000, 230000, 250000 };
        private decimal[] Purchases = { 80000, 85000, 95000, 90000, 100000, 105000, 110000, 100000, 110000, 115000, 120000, 130000 };
        private decimal[] Expenses = { 30000, 30000, 35000, 32000, 35000, 38000, 40000, 37000, 39000, 41000, 42000, 45000 };

        private decimal[] CalcOutput => Sales.Select(s => s * 0.15m).ToArray();
        private decimal[] CalcInput => Purchases.Select(p => p * 0.15m).ToArray();
        private decimal[] CalcNet => CalcOutput.Zip(CalcInput, (o, i) => o - i).ToArray();

        // Adjusted arrays (with sample overrides for demo)
        private decimal[] AdjOutput => CalcOutput.Select((val, idx) => idx == 1 ? val + 500m : val).ToArray();
        private decimal[] AdjInput => CalcInput.Select((val, idx) => idx == 2 ? val - 250m : val).ToArray();
        private decimal[] AdjNet => AdjOutput.Zip(AdjInput, (o, i) => o - i).ToArray();

        // ====================================================
        // PRIVATE FIELDS - STATE
        // ====================================================
        private bool IsLoading { get; set; } = true;
        private bool blAlert { get; set; } = true;
        private ViqAlertComponent.AlertSeverity AlertSeverity { get; set; } = ViqAlertComponent.AlertSeverity.Warning;
        private string AlertHeading { get; set; } = "VAT:";
        private string AlertMessage { get; set; } = "Supply VAT calculation details and adjustments in this section.";

        // ====================================================
        // LIFECYCLE
        // ====================================================
        protected override async Task OnInitializedAsync()
        {
            try
            {
                AssessmentId = sessionService?.AssessmentId ?? 0;

                Logger?.LogInformation(
                    "AssessmentVATPage initialized for assessment {AssessmentId}",
                    AssessmentId);

                // Load VAT data if needed
                // await LoadAndMapVATData();
                // await CreateSummaries();

                IsLoading = false;

                // Subscribe to projection changes
                if (projectionStateManager != null)
                {
                    projectionStateManager.ProjectionChanged += OnProjectionChanged;
                    Logger?.LogDebug("AssessmentVATPage subscribed to ProjectionChanged events");
                }
            }
            catch (Exception ex)
            {
                Logger?.LogError(ex, "Error initializing AssessmentVATPage");
                IsLoading = false;
            }
        }

        // ====================================================
        // EVENT HANDLERS
        // ====================================================

        
        /// Handle submit data action
        
        private async Task HandleSubmitData()
        {
            try
            {
                // Execute your SQL Stored Procedures or API services here
                // e.g., await VATService.SaveAsync(AssessmentId, model);

                var result = new SaveResult
                {
                    Success = true,
                    ClosePanel = true,  // true closes drawer, false keeps it open
                    Message = "VAT configuration updated successfully."
                };

                // Fire event back to main orchestrator page
                await OnSaveComplete.InvokeAsync(result);
            }
            catch (Exception ex)
            {
                await OnSaveComplete.InvokeAsync(new SaveResult
                {
                    Success = false,
                    Message = $"Save aborted: {ex.Message}"
                });
            }
        }

        
        /// Open VAT Adjustment OffCanvas using ZabOffCanvasService
        
        private async Task OpenAdjustmentOffCanvas()
        {
            try
            {
                if (zabCanvasService == null)
                {
                    _Toast?.ShowError("Canvas service not available", "Error");
                    return;
                }

                // ✅ Show VAT Adjustment component using ZabOffCanvasService
                await zabCanvasService.ShowAsync(new CanvasRequest
                {
                    Title = "Adjust VAT Schedule",
                    Width = 400,
                    ComponentType = typeof(VatAdjustmentComponent),
                    Parameters = new Dictionary<string, object>
                    {                        
                        { "AssessmentId", sessionService.AssessmentId }
                    },                    
                    ResultCallback = async (result) => await ProcessVATAdjustmentResult(result)
                });

                Logger?.LogInformation("VAT Adjustment OffCanvas opened for assessment {AssessmentId}", AssessmentId);
            }
            catch (Exception ex)
            {
                Logger?.LogError(ex, "Error opening VAT adjustment offcanvas");
                _Toast?.ShowError($"Error opening adjustment panel: {ex.Message}", "Error");
            }
        }

        
        /// Handle VAT adjustments saved from offcanvas
        
        private async Task ProcessVATAdjustmentResult(SaveResult result)
        {
            try
            {
                if (result.Success)
                {
                    _Toast?.ShowSuccess(result.Message ?? "VAT adjustments saved successfully", "Success");
                    Logger?.LogInformation("VAT adjustments saved for assessment {AssessmentId}", AssessmentId);

                    // Refresh table or recompute values after adjustments
                    StateHasChanged();
                }
                else
                {
                    _Toast?.ShowError(result.Message ?? "Failed to save VAT adjustments", "Error");
                    Logger?.LogWarning("VAT adjustment save failed: {Message}", result.Message);
                }
            }
            catch (Exception ex)
            {
                Logger?.LogError(ex, "Error processing VAT adjustment result");
                _Toast?.ShowError($"Error processing result: {ex.Message}", "Error");
            }

            await Task.CompletedTask;
        }

        
        /// Export VAT report (placeholder)
        
        private void ExportReport()
        {
            try
            {
                _Toast?.ShowInfo("Export functionality coming soon", "Export");
                Logger?.LogInformation("Export report requested for assessment {AssessmentId}", AssessmentId);
            }
            catch (Exception ex)
            {
                Logger?.LogError(ex, "Error exporting VAT report");
                _Toast?.ShowError($"Error exporting report: {ex.Message}", "Error");
            }
        }

        
        /// Save all changes to database
        
        private async Task SaveChanges()
        {
            try
            {
                _Toast?.ShowInfo("Saving VAT changes...", "Save");
                Logger?.LogInformation("Saving VAT changes for assessment {AssessmentId}", AssessmentId);

                // Implement actual save logic here
                // await VATService.SaveAsync(AssessmentId, vatData);

                _Toast?.ShowSuccess("VAT data saved successfully", "Success");
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                Logger?.LogError(ex, "Error saving VAT changes");
                _Toast?.ShowError($"Error saving changes: {ex.Message}", "Error");
            }
        }

        
        /// Handle projection state changes
        
        private void OnProjectionChanged(object? sender, ProjectionChangedEventArgs e)
        {
            try
            {
                if (e != null && e.AssessmentId == AssessmentId && e.DataType == "VAT")
                {
                    Logger?.LogInformation(
                        "VAT data changed externally, refreshing for assessment {AssessmentId}",
                        AssessmentId);

                    // Refresh VAT data
                    InvokeAsync(async () =>
                    {
                        // await LoadAndMapVATData();
                        StateHasChanged();
                    });
                }
            }
            catch (Exception ex)
            {
                Logger?.LogError(ex, "Error handling projection change");
            }
        }

        // ====================================================
        // PRIVATE HELPER METHODS
        // ====================================================

        
        /// Load and map VAT data from database (placeholder)
        
        private async Task LoadAndMapVATData()
        {
            try
            {
                // TODO: Implement actual VAT data loading
                // var vatData = await VATService.GetAsync(AssessmentId);
                // Map to local arrays or objects

                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                Logger?.LogError(ex, "Error loading VAT data");
            }
        }

        
        /// Create VAT summaries (placeholder)
        
        private async Task CreateSummaries()
        {
            try
            {
                // TODO: Implement summary calculations
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                Logger?.LogError(ex, "Error creating VAT summaries");
            }
        }

        // ====================================================
        // CLEANUP
        // ====================================================

        async ValueTask IAsyncDisposable.DisposeAsync()
        {
            // Unsubscribe from events
            if (projectionStateManager != null)
            {
                projectionStateManager.ProjectionChanged -= OnProjectionChanged;
            }
            await Task.CompletedTask;
        }
    }
}
