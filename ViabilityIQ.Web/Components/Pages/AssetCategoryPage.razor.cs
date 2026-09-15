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
    
    /// AssetCategoryPage - List and manage asset categories
    /// Categories are used to organize assets on the balance sheet (Fixed Assets, Current Assets, Intangible Assets, etc.)
    
    public partial class AssetCategoryPage : IAsyncDisposable
    {
        // ====================================================
        // INJECTIONS
        // ====================================================
        [Inject]
        private IGenericDataRepository<AssetCategory> categoryRepository { get; set; } = default!;

        [Inject]
        ISessionService? sessionService { get; set; }

        [Inject]
        ToastService? _Toast { get; set; }

        [Inject]
        OffCanvasStateService? OffcanvasService { get; set; } = default!;

        [Inject]
        private IJSRuntime JS { get; set; } = default!;

        [Inject]
        private IPdfExportService PdfService { get; set; } = default!;

        [Inject]
        private IExcelEPPlusExportService ExcelService { get; set; } = default!;

        // ====================================================
        // PRIVATE FIELDS - ALERT/INFO
        // ====================================================
        private ViqAlertComponent.AlertSeverity AlertSeverity = ViqAlertComponent.AlertSeverity.Info;
        private string AlertHeading = "Asset Categories";
        private string AlertMessage = "Manage asset categories for organizing and classifying assets on the balance sheet. Categories determine default depreciation settings and balance sheet presentation.";

        // ====================================================
        // PRIVATE FIELDS - DATA
        // ====================================================
        private List<AssetCategory> categoriesList = new();
        private List<ZabDataTableAdvanced<AssetCategory>.ColumnDefinition<AssetCategory>> tableColumns = new();

        // ====================================================
        // PRIVATE FIELDS - STATE
        // ====================================================
        private bool loadingStateActive = false;

        // ====================================================
        // LIFECYCLE METHODS
        // ====================================================

        
        /// Initialize component - setup table columns and load data
        
        protected override async Task OnInitializedAsync()
        {
            // ✅ Subscribe to OffCanvas callbacks
            OffcanvasService!.OnShow += HandleCanvasShow;

            _ = LoadGridDatasetAsync();

            // ✅ Define table columns
            tableColumns = new List<ZabDataTableAdvanced<AssetCategory>.ColumnDefinition<AssetCategory>>
            {
                new()
                {
                    Title = "Category Name",
                    Value = x => x.CategoryName ?? ""
                },
                new()
                {
                    Title = "Category Type",
                    Value = x => x.IsCurrentAsset ? "Current Asset" : "Fixed Asset",
                    UseBadge = true,
                    BadgeClass = x => x.IsCurrentAsset ? "bg-info text-black" : "bg-warning text-black"
                },
                new()
                {
                    Title = "Balance Sheet Section",
                    Value = x => x.BalanceSheetSection ?? "N/A"
                },
                new()
                {
                    Title = "Description",
                    Value = x => x.Description ?? ""
                },
                new()
                {
                    Title = "Display Order",
                    Value = x => x.DisplayOrder.ToString(),
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

        
        /// Handle when canvas opens
        
        private async Task HandleCanvasShow(CanvasRequest request)
        {
            await Task.CompletedTask;
        }

        
        /// Load all asset categories from database
        
        private async Task LoadGridDatasetAsync()
        {
            loadingStateActive = true;
            StateHasChanged();

            try
            {
                var resultSet = await categoryRepository.GetAllAsync();
                categoriesList = resultSet != null && resultSet.Any() ? resultSet.ToList() : new List<AssetCategory>();
            }
            catch (Exception ex)
            {
                _Toast?.ShowError($"Failed to load asset categories: {ex.Message}", "Error");
                System.Diagnostics.Debug.WriteLine($"[AssetCategoryPage] Error loading categories: {ex}");
            }
            finally
            {
                loadingStateActive = false;
                StateHasChanged();
            }
        }

        
        /// Open form to add or edit a category
        
        private async Task HandleFormExecution(long extractedRecordId)
        {
            string formTitle = extractedRecordId == 0 ? "Add Asset Category" : "Edit Asset Category";

            await OffcanvasService!.ShowAsync(new CanvasRequest
            {
                Title = formTitle,
                Width = 500,
                ComponentType = typeof(AssetCategoryFormComponent),
                Parameters = new Dictionary<string, object>
                {
                    { "AssetCategoryId", extractedRecordId }
                },
                ResultCallback = async (result) => await ProcessExecutionFeedback(result)
            });
        }

        
        /// Delete a selected asset category
        
        private async Task DeleteSelectedCategory(AssetCategory targetCategory)
        {
            try
            {
                var success = await categoryRepository.DeleteAsync(targetCategory);
                if (success)
                {
                    _Toast!.ShowSuccess("Asset category has been deleted successfully.", sessionService!.AppTitle);
                    await LoadGridDatasetAsync();
                }
                else
                {
                    _Toast!.ShowError("Failed to delete asset category.", "Error");
                }
            }
            catch (Exception ex)
            {
                _Toast!.ShowError($"Error deleting category: {ex.Message}", "Error");
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

        
        /// Export categories to PDF
        
        private async Task ExecutePrintFormatProcess(List<AssetCategory> targetedDataset)
        {
            try
            {
                loadingStateActive = true;
                StateHasChanged();

                var printDataSet = targetedDataset.Select(item => new CategoryPrintDto
                {
                    CategoryName = item.CategoryName,
                    CategoryType = item.IsCurrentAsset ? "Current Asset" : "Fixed Asset",
                    BalanceSheetSection = item.BalanceSheetSection,
                    Description = item.Description,
                    DisplayOrder = item.DisplayOrder,
                    Status = item.Active ? "Active" : "Inactive"
                }).ToList();

                byte[] pdfBytes = await PdfService.GenerateReportDataPdfAsync(printDataSet, "Asset Categories Report");
                string fileName = $"Asset_Categories_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";

                await JS.InvokeVoidAsync("ZabFileSaver.DownloadBinaryStream", fileName, Convert.ToBase64String(pdfBytes));
                _Toast!.ShowSuccess("PDF report generated successfully.", sessionService!.AppTitle);
            }
            catch (Exception ex)
            {
                _Toast!.ShowError($"PDF generation failed: {ex.Message}", sessionService!.AppTitle);
                System.Diagnostics.Debug.WriteLine($"[AssetCategoryPage] Error generating PDF: {ex}");
            }
            finally
            {
                loadingStateActive = false;
                StateHasChanged();
            }
        }

        
        /// Export categories to Excel
        
        private async Task ExecuteExcelExportProcess(List<AssetCategory> targetedDataset)
        {
            try
            {
                loadingStateActive = true;
                StateHasChanged();

                byte[] excelBytes = await ExcelService.GenerateDataReportExcelAsync(targetedDataset, "Asset Categories");
                string fileName = $"Asset_Categories_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

                await JS.InvokeVoidAsync("ZabFileSaver.DownloadBinaryStream", fileName, Convert.ToBase64String(excelBytes));
                _Toast!.ShowSuccess("Excel export completed successfully.", sessionService!.AppTitle);
            }
            catch (Exception ex)
            {
                _Toast!.ShowError($"Excel export failed: {ex.Message}", sessionService!.AppTitle);
                System.Diagnostics.Debug.WriteLine($"[AssetCategoryPage] Error exporting Excel: {ex}");
            }
            finally
            {
                loadingStateActive = false;
                StateHasChanged();
            }
        }

        
        /// Email distribution process (placeholder)
        
        private async Task ExecuteEmailDistributionProcess(List<AssetCategory> targetedDataset)
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
        
        public class CategoryPrintDto
        {
            [DisplayName("Category Name")]
            public string? CategoryName { get; set; }

            [DisplayName("Category Type")]
            public string? CategoryType { get; set; }

            [DisplayName("Balance Sheet Section")]
            public string? BalanceSheetSection { get; set; }

            [DisplayName("Description")]
            public string? Description { get; set; }

            [DisplayName("Display Order")]
            public int DisplayOrder { get; set; }

            [DisplayName("Status")]
            public string? Status { get; set; }
        }
    }
}
