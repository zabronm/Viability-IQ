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
    public partial class ClientTypePage
    {
        [Inject] private IGenericDataRepository<ClientType> sectorRepository { get; set; } = default!;
        [Inject] ISessionService? sessionService { get; set; }
        [Inject] ToastService? _Toast { get; set; }
        [Inject] private IJSRuntime JS { get; set; } = default!;
        [Inject] private IPdfExportService PdfService { get; set; } = default!;
        [Inject] private IExcelEPPlusExportService ExcelService { get; set; } = default!;
       
        // Alert
        //---------------------------------------------------------
        private bool blAlert = true;
        private ViqAlertComponent.AlertSeverity AlertSeverity = ViqAlertComponent.AlertSeverity.Info;
        private string AlertHeading = "Client Types/Categories";
        private string AlertMessage = "Register all classifications of your clients, these will be assumed by your assessment reports and dashboards.";

        private List<ClientType> clientTypeList = new();
        private List<ZabDataTableAdvanced<ClientType>.ColumnDefinition<ClientType>> tableColumns = new();

        private ZabOffCanvas? canvasShell;
        private bool canvasOpenStatus = false;
        private string formTitle = "Client Type";
        private long activeRecordId = 0;
        private bool loadingStateActive = false;

        protected override async Task OnInitializedAsync()
        {
            _ = LoadGridDatasetAsync();

            tableColumns = new List<ZabDataTableAdvanced<ClientType>.ColumnDefinition< ClientType>>
            {
                new() { Title = "Client Type", Value = x => x.ClientTypeName ?? "" },
                new() { Title = "Remarks / Notes", Value = x => x.Remarks ?? "" },
                new() {
                    Title = "Status",
                    Value = x => x.Active == true ? "Active" : "Inactive",
                    UseBadge = true,
                    BadgeClass = x => x.Active == true ? "badge-approved" : "badge-rejected"
                }
            };

           await  Task.CompletedTask;
        }

        private async Task LoadGridDatasetAsync()
        {
            loadingStateActive = true;
            StateHasChanged();

            try
            {
                var resultSet = await sectorRepository.GetAllAsync();
                clientTypeList = resultSet != null && resultSet.Any() ? resultSet.ToList() : new List< ClientType>();
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
            formTitle = extractedRecordId == 0 ? "Add client type" : "Modify client type";

            if (canvasShell != null)
            {
                await canvasShell.OpenAsync(formTitle);
            }
        }

        private async Task DeleteSelectedSector( ClientType targetModel)
        {
            var success = await sectorRepository.DeleteAsync(targetModel);
            if (success)
            {
                _Toast!.ShowSuccess("Client type removed.", sessionService!.AppTitle);
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

        private async Task ExecutePrintFormatProcess(List<ClientType> targetedDataset)
        {
            try
            {
                loadingStateActive = true;
                StateHasChanged();

                var printDataSet = targetedDataset.Select(item => new ClientPrintDto
                {
                    ClientTypeName    = item.ClientTypeName,
                    Remarks = item.Remarks,
                    Status = item.Active == true ? "Active" : "Inactive"
                }).ToList();

                byte[] pdfBytes = await PdfService.GenerateReportDataPdfAsync(printDataSet, "Client Category List");
                string fileName = $"Client_Categories_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";

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

        private async Task ExecuteExcelExportProcess(List< ClientType> targetedDataset)
        {
            try
            {
                loadingStateActive = true;
                byte[] excelBytes = await ExcelService.GenerateDataReportExcelAsync(targetedDataset, "Client Type Master Records");
                string fileName = $"Client_Category_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

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

        private async Task ExecuteEmailDistributionProcess(List< ClientType> targetedDataset)
        {
            await Task.CompletedTask;
        }

        public class ClientPrintDto
        {
            [DisplayName("Client Type")]
            public string?ClientTypeName { get; set; }
            public string? Remarks { get; set; }
            [DisplayName("Status")]
            public string? Status { get; set; }
        }
    }
}