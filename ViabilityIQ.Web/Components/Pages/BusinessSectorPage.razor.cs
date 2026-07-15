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
using ViabilityIQ.Web.Services;


namespace ViabilityIQ.Web.Components.Pages
{
    public partial class BusinessSectorPage
    {
        [Inject] private IGenericDataRepository<BusinessSector> sectorRepository { get; set; } = default!;
        [Inject] ISessionService? sessionService { get; set; }
        [Inject] ToastService? _Toast { get; set; }
        [Inject] private IJSRuntime JS { get; set; } = default!;
        [Inject] private IPdfExportService PdfService { get; set; } = default!;
        [Inject] private IExcelEPPlusExportService ExcelService { get; set; } = default!;
       
        // Alert
        //---------------------------------------------------------
        private bool blAlert = true;
        private ViqAlertComponent.AlertSeverity AlertSeverity = ViqAlertComponent.AlertSeverity.Info;
        private string AlertHeading = "Business Sectors";
        private string AlertMessage = "Setup all base GLOBAL business sectors that will be assumed and/or inherited by your assessments.";

        private List<BusinessSector> sectorList = new();
        private List<ZabDataTableAdvanced<BusinessSector>.ColumnDefinition<BusinessSector>> tableColumns = new();

        private ZabOffCanvas? canvasShell;
        private bool canvasOpenStatus = false;
        private string formTitle = "Business Sector";
        private long activeRecordId = 0;
        private bool loadingStateActive = false;

        protected override async Task OnInitializedAsync()
        {
            _ = LoadGridDatasetAsync();

            tableColumns = new List<ZabDataTableAdvanced<BusinessSector>.ColumnDefinition<BusinessSector>>
            {
                new() { Title = "Sector Name", Value = x => x.BusinessSectorName ?? "" },
                new() { Title = "Remarks / Notes", Value = x => x.Remarks ?? "" },
                new() {
                    Title = "Status",
                    Value = x => x.Active == true ? "Active" : "Inactive",
                    UseBadge = true,
                    BadgeClass = x => x.Active == true ? "badge-approved" : "badge-rejected"
                }
            };
        }

        private async Task LoadGridDatasetAsync()
        {
            loadingStateActive = true;
            StateHasChanged();

            try
            {
                var resultSet = await sectorRepository.GetAllAsync();
                sectorList = resultSet != null && resultSet.Any() ? resultSet.ToList() : new List<BusinessSector>();
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
            formTitle = extractedRecordId == 0 ? "Add Business Sector" : "Modify Business Sector";

            if (canvasShell != null)
            {
                await canvasShell.OpenAsync(formTitle);
            }
        }

        private async Task DeleteSelectedSector(BusinessSector targetModel)
        {
            var success = await sectorRepository.DeleteAsync(targetModel);
            if (success)
            {
                _Toast!.ShowSuccess("Business sector entry permanently removed.", sessionService!.AppTitle);
                await LoadGridDatasetAsync();
            }
        }

        async Task ProcessExecutionFeedback(SaveResult _result)
        {
            if (_result.Success)
            {
                _Toast!.ShowSuccess(_result.Message, sessionService!.AppTitle);
            }
            else
            {
                _Toast!.ShowError(_result.Message, "Error encountered:");
            }

            if (_result.ClosePanel && canvasShell != null)
            {
                await canvasShell.CloseAsync();
            }

            await LoadGridDatasetAsync();
            StateHasChanged();
        }

        private async Task ExecutePrintFormatProcess(List<BusinessSector> targetedDataset)
        {
            try
            {
                loadingStateActive = true;
                StateHasChanged();

                var printDataSet = targetedDataset.Select(item => new SectorPrintDto
                {
                    BusinessSectorName = item.BusinessSectorName,
                    Remarks = item.Remarks,
                    Status = item.Active == true ? "Active" : "Inactive"
                }).ToList();

                byte[] pdfBytes = await PdfService.GenerateReportDataPdfAsync(printDataSet, "Business Sector Registry Ledger");
                string fileName = $"Business_Sectors_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";

                await JS.InvokeVoidAsync("ZabFileSaver.DownloadBinaryStream", fileName, Convert.ToBase64String(pdfBytes));
                _Toast!.ShowSuccess("PDF report generated successfully.", sessionService!.AppTitle);
            }
            catch (Exception ex)
            {
                _Toast!.ShowError($"PDF engine failure: {ex.Message}", sessionService!.AppTitle);
            }
            finally
            {
                loadingStateActive = false;
                StateHasChanged();
            }
        }

        private async Task ExecuteExcelExportProcess(List<BusinessSector> targetedDataset)
        {
            try
            {
                loadingStateActive = true;
                byte[] excelBytes = await ExcelService.GenerateDataReportExcelAsync(targetedDataset, "Business Sector Master Records");
                string fileName = $"Business_Sectors_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

                await JS.InvokeVoidAsync("ZabFileSaver.DownloadBinaryStream", fileName, Convert.ToBase64String(excelBytes));
                _Toast!.ShowSuccess("Spreadsheet file compiled successfully.", sessionService!.AppTitle);
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

        private async Task ExecuteEmailDistributionProcess(List<BusinessSector> targetedDataset)
        {
            await Task.CompletedTask;
        }

        public class SectorPrintDto
        {
            [DisplayName("Sector Name")]
            public string? BusinessSectorName { get; set; }
            public string? Remarks { get; set; }
            [DisplayName("Status")]
            public string? Status { get; set; }
        }
    }
}