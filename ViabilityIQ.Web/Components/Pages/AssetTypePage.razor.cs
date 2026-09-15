using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using ViabilityIQ.Application.Interfaces;
using ViabilityIQ.Shared.DataModels;
using ViabilityIQ.Shared.SharedModels;
using ViabilityIQ.Web.Components.CommonComponents;
using ViabilityIQ.Web.Components.Pages.PageFormComponents;
using ViabilityIQ.Web.Components.Pages_Assessments.AssetFormComponents;
using ViabilityIQ.Web.Services;

namespace ViabilityIQ.Web.Components.Pages
{
    
    /// AssetTypePage - List and manage asset types
    /// Asset types are specific classifications within categories with default depreciation settings  
    public partial class AssetTypePage : IAsyncDisposable
    {       
        [Inject]        private IGenericDataRepository<AssetType> typeRepository { get; set; } = default!;
        [Inject]        private IGenericDataRepository<AssetCategory> categoryRepository { get; set; } = default!;
        [Inject]        ISessionService? sessionService { get; set; }
        [Inject]        ToastService? _Toast { get; set; }
        [Inject]        OffCanvasStateService? OffcanvasService { get; set; } = default!;
        [Inject]        private IJSRuntime JS { get; set; } = default!;
        [Inject]        private IPdfExportService PdfService { get; set; } = default!;
        [Inject]        private IExcelEPPlusExportService ExcelService { get; set; } = default!;

        
        private ViqAlertComponent.AlertSeverity AlertSeverity = ViqAlertComponent.AlertSeverity.Info;
        private string AlertHeading = "Asset Types";
        private string AlertMessage = "Manage specific asset types within categories. Each type can have default depreciation settings that apply to new assets of that type.";
              
        private List<AssetType> typesList = new();
        private List<AssetCategory> categoriesList = new();
        private List<ZabDataTableAdvanced<AssetType>.ColumnDefinition<AssetType>> tableColumns = new();       
        private bool loadingStateActive = false;

      
        /// Initialize component - setup table columns and load data        
        protected override async Task OnInitializedAsync()
        {
            // ✅ Subscribe to OffCanvas callbacks
            OffcanvasService!.OnShow += HandleCanvasShow;

            _ = LoadGridDatasetAsync();

            // ✅ Define table columns
            tableColumns = new List<ZabDataTableAdvanced<AssetType>.ColumnDefinition<AssetType>>
            {
                new()
                {
                    Title = "Type Name",
                    Value = x => x.TypeName ?? ""
                },
                new()
                {
                    Title = "Category",
                    CellTemplate = context => builder => {
                        var category = categoriesList.FirstOrDefault(c => c.AssetCategoryId == context.AssetCategoryId);
                        builder.AddContent(0, category?.CategoryName ?? "Unknown");
                    }
                },
                new()
                {
                    Title = "Depreciable",
                    Value = x => x.IsDepreciable ? "Yes" : "No",
                    UseBadge = true,
                    BadgeClass = x => x.IsDepreciable ? "bg-success text-white" : "bg-secondary text-white"
                },
                new()
                {
                    Title = "Asset Class",
                    Value = x => x.IsCurrent ? "Current" : "Fixed",
                    UseBadge = true,
                    BadgeClass = x => x.IsCurrent ? "bg-info text-black" : "bg-warning text-black"
                },
                new()
                {
                    Title = "Depr. Rate (%)",
                    Value = x => x.IsDepreciable ? $"{x.DefaultDepreciationRate:F2}%" : "N/A",
                    CssClass = "text-end"
                },
                new()
                {
                    Title = "Useful Life (Yrs)",
                    Value = x => x.DefaultUsefulLifeYears.HasValue ? x.DefaultUsefulLifeYears.ToString() : "N/A",
                    CssClass = "text-center"
                },
                new()
                {
                    Title = "Status",
                    Value = x => x.Active == true ? "Active" : "Inactive",
                    UseBadge = true,
                    BadgeClass = x => x.Active == true ? "bg-success text-white" : "bg-secondary text-white"
                }
            };
        }

        // ====================================================
        // EVENT HANDLERS
        // ====================================================
        // 
        /// Handle when canvas opens      
        private async Task HandleCanvasShow(CanvasRequest request)
        {
            await Task.CompletedTask;
        }

        
        /// Load all asset types and categories from database        
        private async Task LoadGridDatasetAsync()
        {
            loadingStateActive = true;
            StateHasChanged();

            try
            {
                var typeResultSet = await typeRepository.GetAllAsync();
                typesList = typeResultSet != null && typeResultSet.Any() ? typeResultSet.ToList() : new List<AssetType>();

                var categoryResultSet = await categoryRepository.GetAllAsync();
                categoriesList = categoryResultSet != null && categoryResultSet.Any() ? categoryResultSet.ToList() : new List<AssetCategory>();
            }
            catch (Exception ex)
            {
                _Toast?.ShowError($"Failed to load asset types: {ex.Message}", "Error");
                System.Diagnostics.Debug.WriteLine($"[AssetTypePage] Error loading types: {ex}");
            }
            finally
            {
                loadingStateActive = false;
                StateHasChanged();
            }
        }

        
        /// Open form to add or edit an asset type
        
        private async Task HandleFormExecution(long extractedRecordId)
        {
            string formTitle = extractedRecordId == 0 ? "Add Asset Type" : "Edit Asset Type";

            await OffcanvasService!.ShowAsync(new CanvasRequest
            {
                Title = formTitle,
                Width = 500,
                ComponentType = typeof(AssetTypeFormComponent),
                Parameters = new Dictionary<string, object>
                {
                    { "AssetTypeId", extractedRecordId }
                },
                ResultCallback = async (result) => await ProcessExecutionFeedback(result)
            });
        }

        
        /// Delete a selected asset type
        
        private async Task DeleteSelectedType(AssetType targetType)
        {
            try
            {
                var success = await typeRepository.DeleteAsync(targetType);
                if (success)
                {
                    _Toast!.ShowSuccess("Asset type has been deleted successfully.", sessionService!.AppTitle);
                    await LoadGridDatasetAsync();
                }
                else
                {
                    _Toast!.ShowError("Failed to delete asset type.", "Error");
                }
            }
            catch (Exception ex)
            {
                _Toast!.ShowError($"Error deleting type: {ex.Message}", "Error");
            }
        }

        
        /// Process feedback from form execution
        
        private async Task ProcessExecutionFeedback(SaveResult _result)
        {
            if (_result.Success)
            {
                _Toast!.ShowSuccess(_result.Message, sessionService!.AppTitle);
                await LoadGridDatasetAsync();
            }
            else
            {
                _Toast!.ShowError(_result.Message, "Error encountered");
            }

            StateHasChanged();
        }

        
        /// Export asset types to PDF
        
        private async Task ExecutePrintFormatProcess(List<AssetType> targetedDataset)
        {
            try
            {
                loadingStateActive = true;
                StateHasChanged();

                var printDataSet = targetedDataset.Select(item => new TypePrintDto
                {
                    TypeName = item.TypeName,
                    CategoryName = categoriesList.FirstOrDefault(c => c.AssetCategoryId == item.AssetCategoryId)?.CategoryName ?? "Unknown",
                    IsDepreciable = item.IsDepreciable ? "Yes" : "No",
                    DefaultDepreciationRate = item.IsDepreciable ? $"{item.DefaultDepreciationRate:F2}%" : "N/A",
                    DefaultUsefulLifeYears = item.DefaultUsefulLifeYears.HasValue ? item.DefaultUsefulLifeYears.ToString() : "N/A",
                    AssetClass = item.IsCurrent ? "Current" : "Fixed",
                    Status = item.Active ? "Active" : "Inactive"
                }).ToList();

                byte[] pdfBytes = await PdfService.GenerateReportDataPdfAsync(printDataSet, "Asset Types Report");
                string fileName = $"Asset_Types_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";

                await JS.InvokeVoidAsync("ZabFileSaver.DownloadBinaryStream", fileName, Convert.ToBase64String(pdfBytes));
                _Toast!.ShowSuccess("PDF report generated successfully.", sessionService!.AppTitle);
            }
            catch (Exception ex)
            {
                _Toast!.ShowError($"PDF generation failed: {ex.Message}", sessionService!.AppTitle);
                System.Diagnostics.Debug.WriteLine($"[AssetTypePage] Error generating PDF: {ex}");
            }
            finally
            {
                loadingStateActive = false;
                StateHasChanged();
            }
        }

        
        /// Export asset types to Excel
        
        private async Task ExecuteExcelExportProcess(List<AssetType> targetedDataset)
        {
            try
            {
                loadingStateActive = true;
                StateHasChanged();

                byte[] excelBytes = await ExcelService.GenerateDataReportExcelAsync(targetedDataset, "Asset Types");
                string fileName = $"Asset_Types_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

                await JS.InvokeVoidAsync("ZabFileSaver.DownloadBinaryStream", fileName, Convert.ToBase64String(excelBytes));
                _Toast!.ShowSuccess("Excel export completed successfully.", sessionService!.AppTitle);
            }
            catch (Exception ex)
            {
                _Toast!.ShowError($"Excel export failed: {ex.Message}", sessionService!.AppTitle);
                System.Diagnostics.Debug.WriteLine($"[AssetTypePage] Error exporting Excel: {ex}");
            }
            finally
            {
                loadingStateActive = false;
                StateHasChanged();
            }
        }

        
        /// Email distribution process (placeholder)
        
        private async Task ExecuteEmailDistributionProcess(List<AssetType> targetedDataset)
        {
            await Task.CompletedTask;
        }

        
        /// Cleanup - unsubscribe from events
        
        async ValueTask IAsyncDisposable.DisposeAsync()
        {
            if (OffcanvasService != null)
            {
                OffcanvasService.OnShow -= HandleCanvasShow;
            }
        }

        // ====================================================
        // PRINT DTO
        // ====================================================

        
        /// Print Data Transfer Object for PDF/Excel export
        
        public class TypePrintDto
        {
            [DisplayName("Type Name")]
            public string? TypeName { get; set; }

            [DisplayName("Category")]
            public string? CategoryName { get; set; }

            [DisplayName("Depreciable")]
            public string? IsDepreciable { get; set; }

            [DisplayName("Depreciation Rate")]
            public string? DefaultDepreciationRate { get; set; }

            [DisplayName("Useful Life (Years)")]
            public string? DefaultUsefulLifeYears { get; set; }

            [DisplayName("Asset Class")]
            public string? AssetClass { get; set; }

            [DisplayName("Status")]
            public string? Status { get; set; }
        }
    }
}
