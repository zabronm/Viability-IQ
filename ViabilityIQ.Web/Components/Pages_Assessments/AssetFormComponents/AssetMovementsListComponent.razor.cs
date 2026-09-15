

using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ViabilityIQ.Application.Dtos;
using ViabilityIQ.Application.Interfaces;
using ViabilityIQ.Shared.DataModels;
using ViabilityIQ.Shared.SharedModels;
using ViabilityIQ.Web.Services;

namespace ViabilityIQ.Web.Components.Pages_Assessments.AssetFormComponents
{
    
    /// AssetMovementsListComponent - Displays asset movements and depreciation schedule
    /// Provides tabbed interface for managing asset transactions and tracking depreciation
    
    public partial class AssetMovementsListComponent : ComponentBase
    {
        // ====================================================
        // PARAMETERS
        // ====================================================
        
        /// The Asset ID to display movements for        
        [Parameter]        public long AssetId { get; set; }
        
        /// The Assessment ID for context        
        [Parameter]        public long AssessmentId { get; set; }

        // ====================================================
        // INJECTIONS
        // ====================================================
        [Inject]        private IGenericDataRepository<AssessmentAsset> assetRepository { get; set; } = default!;
        [Inject]        private IGenericDataRepository<AssessmentAssetMovement> assetMovementRepository { get; set; } = default!;
        [Inject]        private IGenericDataRepository<AssetDepreciation> assetDepreciationRepository { get; set; } = default!;
        [Inject]        private ISessionService? sessionService { get; set; }
        [Inject]        private ToastService? _Toast { get; set; }
        [Inject]        private OffCanvasStateService? OffcanvasService { get; set; } = default!;

        // ====================================================
        // PRIVATE FIELDS - DISPLAY DATA
        // ====================================================
        private AssessmentAsset? currentAsset;
        private List<AssetMovementDto> movementsList = new();
        private List<AssetDepreciationDto> depreciationList = new();

        // ====================================================
        // PRIVATE FIELDS - ASSET SUMMARY
        // ====================================================
        private string assetName = string.Empty;
        private string assetType = string.Empty;
        private string assetCategory = string.Empty;
        private decimal openingValue = 0M;
        private decimal closingValue = 0M;
        private decimal accumulatedDepreciation = 0M;
        private decimal netValue = 0M;

        // ====================================================
        // PRIVATE FIELDS - DEPRECIATION INFO
        // ====================================================
        private string? depreciationMethod;
        private decimal depreciationRate = 0M;
        private int? usefulLifeYears;
        private decimal annualDepreciation = 0M;

        // ====================================================
        // PRIVATE FIELDS - COMPONENT STATE
        // ====================================================
        private string activeTab = "movements";  // "movements" or "depreciation"
        private bool isLoading = false;
        private string successMessage = string.Empty;
        private string errorMessage = string.Empty;

        // ====================================================
        // LIFECYCLE METHODS
        // ====================================================

        
        /// Initialize component - load asset, movements, and depreciation data
        
        protected override async Task OnInitializedAsync()
        {
            isLoading = true;

            try
            {
                // ✅ Load asset details
                currentAsset = await assetRepository.GetByIdAsync(AssetId);
                if (currentAsset == null)
                {
                    _Toast?.ShowError($"Asset with ID {AssetId} not found.", "Error");
                    return;
                }

                // ✅ Load asset movements
                var movements = await assetMovementRepository.GetAllAsync(x => x.AssessmentAssetId == AssetId);
                movementsList = ConvertMovementsToDto(movements.ToList());

                // ✅ Load depreciation schedule
                var depreciation = await assetDepreciationRepository.GetAllAsync(x => x.AssessmentAssetId == AssetId);
                depreciationList = ConvertDepreciationToDto(depreciation.ToList());

                // ✅ Populate asset summary
                PopulateAssetSummary();
            }
            catch (Exception ex)
            {
                _Toast?.ShowError($"Failed to load asset movements: {ex.Message}", "Error");
                System.Diagnostics.Debug.WriteLine($"[AssetMovementsListComponent] Error in OnInitializedAsync: {ex}");
            }
            finally
            {
                isLoading = false;
            }
        }

        // ====================================================
        // PUBLIC EVENT HANDLERS
        // ====================================================

        
        /// Change active tab view
        
        public void ChangeTab(string tabName)
        {
            activeTab = tabName;
            StateHasChanged();
        }

        
        /// Open form to add a new movement
        
        public async Task OpenAddMovementForm()
        {
            if (OffcanvasService == null) return;

            await OffcanvasService.ShowAsync(new CanvasRequest
            {
                Title = "Add Asset Movement",
                Width = 400,
                ComponentType = typeof(AssetMovementFormComponent),
                Parameters = new Dictionary<string, object>
                {
                    { "AssetMovementId", 0L },
                    { "AssetId", AssetId },
                    { "AssessmentId", AssessmentId }
                },
                ResultCallback = async (result) => await ProcessMovementFeedback(result)
            });
        }

        
        /// Open form to edit an existing movement
        
        public async Task OpenEditMovementForm(long assetMovementId)
        {
            if (OffcanvasService == null) return;

            await OffcanvasService.ShowAsync(new CanvasRequest
            {
                Title = "Edit Asset Movement",
                Width = 400,
                ComponentType = typeof(AssetMovementFormComponent),
                Parameters = new Dictionary<string, object>
                {
                    { "AssetMovementId", assetMovementId },
                    { "AssetId", AssetId },
                    { "AssessmentId", AssessmentId }
                },
                ResultCallback = async (result) => await ProcessMovementFeedback(result)
            });
        }

        
        /// Delete a movement record
        
        public async Task DeleteMovement(AssetMovementDto movement)
        {
            bool confirm = await ConfirmDelete($"Are you sure you want to delete this movement dated {movement.MovementDate:MMM dd, yyyy}?");
            if (!confirm) return;

            try
            {
                // ✅ Convert DTO to entity for deletion
                var movementToDelete = new AssessmentAssetMovement
                {
                    AssetMovementId = movement.AssetMovementId,
                    AssessmentAssetId = movement.AssessmentAssetId,
                    AssessmentId = movement.AssessmentId,
                    MovementDate = movement.MovementDate,
                    MovementType = movement.MovementType,
                    MovementAmount = movement.MovementAmount,
                    Remarks = movement.Description,
                    Reference = movement.Reference,
                    Active = movement.Active,
                    CreatedDate = movement.CreatedDate,
                    CreatedBy = movement.CreatedBy,
                    //ModifiedDate = movement.ModifiedDate,
                    //ModifiedBy = movement.ModifiedBy
                };

                var success = await assetMovementRepository.DeleteAsync(movementToDelete);

                if (success)
                {
                    _Toast?.ShowSuccess("Movement deleted successfully.", sessionService?.AppTitle ?? "Success");
                    await ReloadMovements();
                }
                else
                {
                    _Toast?.ShowError("Failed to delete movement.", "Error");
                }
            }
            catch (Exception ex)
            {
                _Toast?.ShowError($"Error deleting movement: {ex.Message}", "Error");
                System.Diagnostics.Debug.WriteLine($"[AssetMovementsListComponent] Error deleting movement: {ex}");
            }
        }

        
        /// View detailed depreciation information
        
        public async Task ViewDepreciationDetail(AssetDepreciationDto depreciation)
        {
            await Task.CompletedTask;
            // In a full implementation, open a detail view
            _Toast?.ShowInfo($"Depreciation: {depreciation.DepreciationAmount:C0} on {depreciation.DepreciationDate:MMM dd, yyyy}", "Details");
        }

        
        /// Export movements and depreciation to CSV
        
        public async Task ExportAsCSV()
        {
            try
            {
                var csvContent = GenerateCSVContent();
                // In a full implementation, trigger file download
                _Toast?.ShowSuccess("CSV export prepared. (Full download feature coming soon)", "Export");
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _Toast?.ShowError($"Error exporting CSV: {ex.Message}", "Error");
            }
        }

        
        /// Export movements and depreciation to PDF
        
        public async Task ExportAsPDF()
        {
            try
            {
                // In a full implementation, generate PDF
                _Toast?.ShowSuccess("PDF export prepared. (Full PDF generation coming soon)", "Export");
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _Toast?.ShowError($"Error exporting PDF: {ex.Message}", "Error");
            }
        }

        // ====================================================
        // PUBLIC UI HELPER METHODS
        // ====================================================

        
        /// Get CSS class for movement type badge
        
        public string GetMovementTypeClass(string movementType)
        {
            return movementType switch
            {
                "Addition" => "bg-success text-white",
                "Disposal" => "bg-danger text-white",
                "Transfer" => "bg-info text-black",
                "Revaluation" => "bg-warning text-black",
                _ => "bg-secondary text-white"
            };
        }

        
        /// Get CSS class for movement amount (color based on positive/negative)
        
        public string GetMovementAmountClass(decimal amount)
        {
            return amount >= 0 ? "text-success" : "text-danger";
        }

        // ====================================================
        // PRIVATE HELPER METHODS
        // ====================================================

        
        /// Populate asset summary information from current asset
        
        private void PopulateAssetSummary()
        {
            if (currentAsset == null) return;

            assetName = currentAsset.AssetName;
            openingValue = currentAsset.OpeningBalanceValue;
           
            accumulatedDepreciation = currentAsset.OpeningAccumulatedDepreciation;
            netValue = currentAsset.OpeningNetBookValue;

            depreciationMethod = currentAsset.DepreciationMethod;
            depreciationRate = currentAsset.DepreciationRate;
            usefulLifeYears = currentAsset.UsefulLifeYears;
            annualDepreciation = currentAsset.OpeningBalanceValue * (currentAsset.DepreciationRate / 100);

            // TODO: Load asset type and category names from repositories
            assetType = "Fixed Asset";
            assetCategory = "Equipment";
        }

        
        /// Convert AssetMovement entities to AssetMovementDto for display
        
        private List<AssetMovementDto> ConvertMovementsToDto(List<AssessmentAssetMovement> movements)
        {
            return movements.Select(m => new AssetMovementDto
            {
                AssetMovementId = m.AssetMovementId,
                AssessmentAssetId = m.AssessmentAssetId,
                AssessmentId = m.AssessmentId,
                AssetName = currentAsset?.AssetName ?? "Unknown",
                MovementDate = m.MovementDate,
                MovementType = m.MovementType,
                MovementAmount = m.MovementAmount,
                Remarks = m.Remarks,
                Reference = m.Reference,                
                Active = m.Active,
                CreatedDate = m.CreatedDate,
                CreatedBy = m.CreatedBy,
                ModifiedDate = m.ModifiedDate,
                ModifiedBy = m.ModifiedBy
            }).ToList();
        }

        
        /// Convert AssetDepreciation entities to AssetDepreciationDto for display
        
        private List<AssetDepreciationDto> ConvertDepreciationToDto(List<AssetDepreciation> depreciations)
        {
            return depreciations.Select(d => new AssetDepreciationDto
            {
                AssetDepreciationId = d.AssetDepreciationId,
                AssessmentAssetId = d.AssessmentAssetId,
                AssessmentId = d.AssessmentId,
                AssetName = currentAsset?.AssetName ?? "Unknown",
                DepreciationDate = d.DepreciationDate,
                DepreciationAmount = d.DepreciationAmount,
                AccumulatedDepreciationBefore = d.AccumulatedDepreciationBefore,
                AccumulatedDepreciationAfter = d.AccumulatedDepreciationAfter,
                Method = d.Method,
                Remarks = d.Remarks,
                CreatedDate = d.CreatedDate,
                CreatedBy = d.CreatedBy
            }).ToList();
        }

        
        /// Reload movements from database
        
        private async Task ReloadMovements()
        {
            try
            {
                var movements = await assetMovementRepository.GetAllAsync(x => x.AssessmentAssetId == AssetId);
                movementsList = ConvertMovementsToDto(movements.ToList());
                StateHasChanged();
            }
            catch (Exception ex)
            {
                _Toast?.ShowError($"Error reloading movements: {ex.Message}", "Error");
            }
        }

        
        /// Process feedback from movement form (add/edit)
        
        private async Task ProcessMovementFeedback(SaveResult result)
        {
            if (result.Success)
            {
                successMessage = result.Message;
                _Toast?.ShowSuccess(result.Message, sessionService?.AppTitle ?? "Success");
                await ReloadMovements();
            }
            else
            {
                errorMessage = result.Message;
                _Toast?.ShowError(result.Message, "Error");
            }

            StateHasChanged();
        }

        
        /// Show delete confirmation dialog
        
        private async Task<bool> ConfirmDelete(string message)
        {
            // In a full implementation, use a confirmation dialog component
            return await Task.FromResult(true);
        }

        
        /// Generate CSV content from movements and depreciation data
        
        private string GenerateCSVContent()
        {
            var csv = new System.Text.StringBuilder();

            csv.AppendLine("Asset Movements & Depreciation Report");
            csv.AppendLine($"Asset: {assetName}");
            csv.AppendLine($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            csv.AppendLine();

            // Movements section
            csv.AppendLine("MOVEMENTS");
            csv.AppendLine("Date,Type,Amount,Reference,Description");
            foreach (var movement in movementsList.OrderByDescending(x => x.MovementDate))
            {
                csv.AppendLine($"{movement.MovementDate:yyyy-MM-dd},{movement.MovementType},{movement.MovementAmount},{movement.Reference},{movement.Description}");
            }

            csv.AppendLine();

            // Depreciation section
            csv.AppendLine("DEPRECIATION SCHEDULE");
            csv.AppendLine("Date,Amount,Accumulated Before,Accumulated After,Method");
            foreach (var depr in depreciationList.OrderBy(x => x.DepreciationDate))
            {
                csv.AppendLine($"{depr.DepreciationDate:yyyy-MM-dd},{depr.DepreciationAmount},{depr.AccumulatedDepreciationBefore},{depr.AccumulatedDepreciationAfter},{depr.Method}");
            }

            return csv.ToString();
        }
    }
}
