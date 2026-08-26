using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using ViabilityIQ.Application.Dtos;
using ViabilityIQ.Application.Interfaces;
using ViabilityIQ.Infrastructure.Repositories;
using ViabilityIQ.Shared.DataModels;
using ViabilityIQ.Shared.SharedModels;
using ViabilityIQ.Web.Components.CommonComponents;
using ViabilityIQ.Web.Components.Pages.PageFormComponents;
using ViabilityIQ.Web.Services;

namespace ViabilityIQ.Web.Components.Pages
{
    public partial class ProductServicePage : IAsyncDisposable
    {
        [Inject] private IGenericDataRepository<ProductService> productRepository { get; set; } = default!;
        [Inject] ISessionService? sessionService { get; set; }
        [Inject] ToastService? _Toast { get; set; }
        [Inject] private IJSRuntime JS { get; set; } = default!;
        [Inject] private IPdfExportService PdfService { get; set; } = default!;
        [Inject] private IExcelEPPlusExportService ExcelService { get; set; } = default!;
        [Inject] private OffCanvasStateService? OffcanvasService { get; set; } = default!;  // ✅ ADD THIS

        // Alert
        //---------------------------------------------------------
        private bool blAlert = true;
        private ViqAlertComponent.AlertSeverity AlertSeverity = ViqAlertComponent.AlertSeverity.Info;
        private string AlertHeading = "Products/Services";
        private string AlertMessage = "Register your main products/services here, which will be inherited by your assessments in sales.";

        private List<ProductService> productList = new();
        private List<ZabDataTableAdvanced<ProductService>.ColumnDefinition<ProductService>> tableColumns = new();

        private bool loadingStateActive = false;

        protected override async Task OnInitializedAsync()
        {
            // ✅ Subscribe to OffCanvas callbacks
            OffcanvasService!.OnShow += HandleCanvasShow;

            _ = LoadGridDatasetAsync();

            tableColumns = new List<ZabDataTableAdvanced<ProductService>.ColumnDefinition<ProductService>>
            {
                new() { Title = "Product Name", Value = x => x.ProductName },
                new() { Title = "Other Name/s", Value = x => x.OtherName },
                new() { Title = "Category", Value = x => x.ProductCategoryId.ToString() },
                new() { Title = "Markup(%)", Value = x => x.MarkupPercentage.ToString() },
                new() {
                    Title = "Status",
                    Value = x => x.Active == true ? "Active" : "Inactive",
                    UseBadge = true,
                    BadgeClass = x => x.Active == true ? "badge-approved" : "badge-rejected"
                }
            };
        }

        // ✅ Handle when canvas opens
        private async Task HandleCanvasShow(CanvasRequest request)
        {
            await Task.CompletedTask;
        }

        private async Task LoadGridDatasetAsync()
        {
            loadingStateActive = true;
            StateHasChanged();

            try
            {
                var resultSet = await productRepository.GetAllAsync();
                productList = resultSet != null && resultSet.Any() ? resultSet.ToList() : new List<ProductService>();
            }
            finally
            {
                loadingStateActive = false;
                StateHasChanged();
            }
        }

        // ✅ Open form via service
        private async Task HandleFormExecution(long extractedRecordId)
        {
            string formTitle = extractedRecordId == 0 ? "Add Product/Service" : "Modify Product/Service";

            await OffcanvasService!.ShowAsync(new CanvasRequest
            {
                Title = formTitle,
                Width = 500,
                ComponentType = typeof(ProductServiceFormComponent),
                Parameters = new Dictionary<string, object>
                {
                    { "ProductServiceId", extractedRecordId }
                },
                ResultCallback = async (result) => await ProcessExecutionFeedback(result)
            });
        }

        private async Task DeleteSelectedProduct(ProductService targetProduct)
        {
            var success = await productRepository.DeleteAsync(targetProduct);
            if (success)
            {
                _Toast!.ShowSuccess("Product/Service removed successfully.", sessionService!.AppTitle);
                await LoadGridDatasetAsync();
            }
            else
            {
                _Toast!.ShowError("Failed to delete product/service.", sessionService!.AppTitle);
            }
        }

        // ✅ Called when form completes
        private async Task ProcessExecutionFeedback(SaveResult _result)
        {
            if (_result.Success)
            {
                _Toast!.ShowSuccess(_result.Message, sessionService!.AppTitle);
                await LoadGridDatasetAsync();
            }
            else
            {
                _Toast!.ShowError(_result.Message, "Error encountered while saving");
            }

            StateHasChanged();
        }

        private async Task ExecutePrintFormatProcess(List<ProductService> targetedDataset)
        {
            try
            {
                loadingStateActive = true;
                StateHasChanged();

                var PrintDataSet = targetedDataset.Select(item => new ProductServicePrintDto
                {
                    ProductServiceName = item.ProductName,
                    OtherName = item.OtherName,
                    MarkupPercentage = item.MarkupPercentage,
                    ProductOrService = item.ProductOrService
                }).ToList();

                byte[] pdfReportBytes = await PdfService.GenerateReportDataPdfAsync(PrintDataSet, "Product/Service Summary");
                string targetFileName = $"Product_Service_Master_Ledger_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";

                await JS.InvokeVoidAsync("ZabFileSaver.DownloadBinaryStream", targetFileName, Convert.ToBase64String(pdfReportBytes));
                _Toast!.ShowSuccess("PDF document spooled to your downloads directory.", sessionService!.AppTitle);
            }
            catch (Exception ex)
            {
                _Toast!.ShowError($"PDF Engine error: {ex.Message}", sessionService!.AppTitle);
            }
            finally
            {
                loadingStateActive = false;
                StateHasChanged();
            }
        }

        private async Task ExecuteExcelExportProcess(List<ProductService> targetedDataset)
        {
            try
            {
                loadingStateActive = true;

                byte[] excelBytes = await ExcelService.GenerateDataReportExcelAsync(targetedDataset, "Product/Service Master Records");
                string fileName = $"Product_Service_Export_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

                await JS.InvokeVoidAsync("ZabFileSaver.DownloadBinaryStream", fileName, Convert.ToBase64String(excelBytes));
                _Toast!.ShowSuccess("Excel spreadsheet compilation completed successfully.", sessionService!.AppTitle);
            }
            catch (Exception ex)
            {
                _Toast!.ShowError($"Excel Export Aborted: {ex.Message}", sessionService!.AppTitle);
            }
            finally
            {
                loadingStateActive = false;
                StateHasChanged();
            }
        }

        private async Task ExecuteEmailDistributionProcess(List<ProductService> targetedDataset)
        {
            try
            {
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _Toast!.ShowError($"Email Transmission Error: {ex.Message}", sessionService!.AppTitle);
            }
            finally
            {
                loadingStateActive = false;
                StateHasChanged();
            }
        }

        // ✅ Cleanup subscriptions
        async ValueTask IAsyncDisposable.DisposeAsync()
        {
            if (OffcanvasService != null)
            {
                OffcanvasService.OnShow -= HandleCanvasShow;
            }
        }

        public class ProductServicePrintDto
        {
            [DisplayName("Product Name")]
            public string? ProductServiceName { get; set; }
            [DisplayName("Other Name/s")]
            public string? OtherName { get; set; }
            [DisplayName("Markup %")]
            public decimal? MarkupPercentage { get; set; }
            [DisplayName("Type")]
            public bool ProductOrService { get; set; }
        }
    }
}