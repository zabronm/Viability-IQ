using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using ViabilityIQ.Application.Dtos;
using ViabilityIQ.Application.Interfaces;
using ViabilityIQ.Shared.DataModels;
using ViabilityIQ.Shared.SharedModels;
using ViabilityIQ.Web.Components.CommonComponents;
using ViabilityIQ.Web.Components.Pages.PageFormComponents;
using ViabilityIQ.Web.Services;

namespace ViabilityIQ.Web.Components.Pages
{
    public partial class BusinessPage : IAsyncDisposable
    {
        [Inject] private IGenericDataRepository<BusinessDto> businessRepository { get; set; } = default!;
        [Inject] private IGenericDataRepository<Business> coreBusinessRepository { get; set; } = default!;
        [Inject] ISessionService? sessionService { get; set; }
        [Inject] ToastService? _Toast { get; set; }
        [Inject] OffCanvasStateService? OffcanvasService { get; set; } = default!;
        [Inject] private IJSRuntime JS { get; set; } = default!;
        [Inject] private IPdfExportService PdfService { get; set; } = default!;
        [Inject] private IExcelEPPlusExportService ExcelService { get; set; } = default!;

        //---------------------------------------------------------
        // Alert
        //---------------------------------------------------------
        private bool blAlert = true;
        private ViqAlertComponent.AlertSeverity AlertSeverity = ViqAlertComponent.AlertSeverity.Info;
        private string AlertHeading = "Businesses";
        private string AlertMessage = "Register a business before it can be assessed. Supply all relevant details that will assist the assessment to be more accurate.";

        private List<BusinessDto> businessList = new();
        private List<ZabDataTableAdvanced<BusinessDto>.ColumnDefinition<BusinessDto>> tableColumns = new();

        private bool loadingStateActive = false;

        protected override async Task OnInitializedAsync()
        {
            // ✅ Subscribe to OffCanvas callbacks
            OffcanvasService!.OnShow += HandleCanvasShow;

            _ = LoadGridDatasetAsync();

            tableColumns = new List<ZabDataTableAdvanced<BusinessDto>.ColumnDefinition<BusinessDto>>
            {
                new() { Title = "Business Name", Value = x => x.BusinessName ?? "" },
                new() { Title = "Sector", Value = x => x.BusinessSectorName ?? "" },
                new() { Title = "Owner", Value = x => x.Client ?? "" },
                new() { Title = "Reg?", Value = x => x.Registered == true ? "Yes" : "No" },
                new() { Title = "VAT Reg?", Value = x => x.VATRegistered == true ? "Yes" : "No" },
                new() { Title = "Province", Value = x => x.ProvinceName ?? "" },
                new() { Title = "Website", Value = x => x.Website ?? "" },
                new() {
                    Title = "Status",
                    Value = x => x.Active == true ? "Active" : "Inactive",
                    UseBadge = true,
                    BadgeClass = x => x.Active == true ? "badge-approved" : "badge-rejected"
                }
            };

            await Task.CompletedTask;
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
                var resultSet = await businessRepository.GetAllAsync();
                businessList = resultSet != null && resultSet.Any() ? resultSet.ToList() : new List<BusinessDto>();
            }
            finally
            {
                loadingStateActive = false;
                StateHasChanged();
            }
        }

        // ✅ Open Business form via service
        private async Task HandleFormExecution(long extractedRecordId)
        {
            string formTitle = extractedRecordId == 0 ? "Add Business Details" : "Modify Business Details";

            await OffcanvasService!.ShowAsync(new CanvasRequest
            {
                Title = formTitle,
                Width = 550,
                ComponentType = typeof(BusinessFormComponent),
                Parameters = new Dictionary<string, object>
                {
                    { "BusinessId", extractedRecordId }
                },
                ResultCallback = async (result) => await ProcessExecutionFeedback(result)
            });
        }

        private async Task DeleteSelectedBusiness(BusinessDto targetDto)
        {
            var trackingPayload = new Business { BusinessId = targetDto.BusinessId };
            var success = await coreBusinessRepository.DeleteAsync(trackingPayload);
            if (success)
            {
                _Toast!.ShowSuccess("Business record has been deleted from system.", sessionService!.AppTitle);
                await LoadGridDatasetAsync();
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

        private async Task ExecutePrintFormatProcess(List<BusinessDto> targetedDataset)
        {
            try
            {
                loadingStateActive = true;
                StateHasChanged();

                var PrintDataSet = targetedDataset.Select(item => new businessPrintDto
                {
                    BusinessName = item.BusinessName,
                    BusinessOwner = item.Client,
                    Province = item.ProvinceName,
                    Telephone = item.Telephone,
                    Mobile = item.Mobile,
                    Email = item.Email,
                    Active = item.Active
                }).ToList();

                byte[] pdfReportBytes = await PdfService.GenerateReportDataPdfAsync(PrintDataSet, "Business Summary");
                string targetFileName = $"Businesses_Summary_List_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";

                await JS.InvokeVoidAsync("ZabFileSaver.DownloadBinaryStream", targetFileName, Convert.ToBase64String(pdfReportBytes));
                _Toast!.ShowSuccess("PDF report downloaded to your downloads directory.", sessionService!.AppTitle);
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

        private async Task ExecuteExcelExportProcess(List<BusinessDto> targetedDataset)
        {
            try
            {
                loadingStateActive = true;

                byte[] excelBytes = await ExcelService.GenerateDataReportExcelAsync(targetedDataset, "Businesses Summary List");
                string fileName = $"Businesses_Summary_List_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

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

        private async Task ExecuteEmailDistributionProcess(List<BusinessDto> targetedDataset)
        {
            await Task.CompletedTask;
        }

        // ✅ Cleanup subscriptions
        async ValueTask IAsyncDisposable.DisposeAsync()
        {
            if (OffcanvasService != null)
            {
                OffcanvasService.OnShow -= HandleCanvasShow;
            }
        }

        public class businessPrintDto
        {
            [DisplayName("Business Name")]
            public string? BusinessName { get; set; }

            [DisplayName("Owner")]
            public string? BusinessOwner { get; set; }
            public string? Province { get; set; }
            public string? Telephone { get; set; }
            public string? Mobile { get; set; }
            public string? Email { get; set; }
            public bool Active { get; set; }
        }
    }
}