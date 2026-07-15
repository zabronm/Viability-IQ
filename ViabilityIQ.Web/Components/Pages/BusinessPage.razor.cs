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
using ViabilityIQ.Web.Services;

namespace ViabilityIQ.Web.Components.Pages
{
    public partial class BusinessPage
    {
        [Inject] private IGenericDataRepository<BusinessDto> businessRepository { get; set; } = default!;
        [Inject] ISessionService? sessionService { get; set; }
        [Inject] ToastService? _Toast { get; set; }
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

        private ZabOffCanvas? canvasShell;
        private bool canvasOpenStatus = false;
        private string formTitle = "Business";
        private long activeRecordId = 0;
        private bool loadingStateActive = false;

        protected override async Task OnInitializedAsync()
        {
            _ = LoadGridDatasetAsync();

            tableColumns = new List<ZabDataTableAdvanced<BusinessDto>.ColumnDefinition<BusinessDto>>
            {
                new() { Title = "Business Name", Value = x => x.BusinessName ?? "" },
                new() { Title = "Sector", Value = x => x.BusinessSectorName ?? "" },
                new() { Title = "Owner", Value = x => x.Client ?? "" },
                new() { Title = "Reg?", Value = x => x.Registered==true? "Yes": "No" },
                new() { Title = "VAT Reg?", Value = x => x.VATRegistered==true? "Yes": "No" },
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

        private async Task HandleFormExecution(long extractedRecordId)
        {
            activeRecordId = extractedRecordId;
            formTitle = extractedRecordId == 0 ? "Add Business Details" : "Modify Business Details";

            if (canvasShell != null)
            {
                await canvasShell.OpenAsync(formTitle);
            }
        }

        // FIXED: Changed incoming signature from Core Business Entity to match BusinessDto requirements 
        private async Task DeleteSelectedBusiness(BusinessDto targetDto)
        {
            // Implementation mapping goes here when tracking records for deletion 
            await Task.CompletedTask;
        }

        async Task ProcessExecutionFeedback(SaveResult _result)
        {
            if (_result.Success)
            {
                _Toast!.ShowSuccess(_result.Message, sessionService!.AppTitle);
            }
            else
            {
                _Toast!.ShowError(_result.Message, "Error encountered while saving");
            }

            if (_result.ClosePanel && canvasShell != null)
            {
                await canvasShell.CloseAsync();
            }

            await LoadGridDatasetAsync();
            StateHasChanged();
        }

        // FIXED: Changed argument type parameter signature from List<Business> to List<BusinessDto>
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

        // FIXED: Changed argument type parameter signature from List<Business> to List<BusinessDto>
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

        // FIXED: Changed argument type parameter signature from List<Business> to List<BusinessDto>
        private async Task ExecuteEmailDistributionProcess(List<BusinessDto> targetedDataset)
        {
            await Task.CompletedTask;
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