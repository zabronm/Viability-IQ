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
    public partial class IncomeTypePage
    {
        [Inject] private IGenericDataRepository<IncomeType> sectorRepository { get; set; } = default!;
        [Inject] ISessionService? sessionService { get; set; }
        [Inject] ToastService? _Toast { get; set; }
        [Inject] private IJSRuntime JS { get; set; } = default!;
        [Inject] private IPdfExportService PdfService { get; set; } = default!;
        [Inject] private IExcelEPPlusExportService ExcelService { get; set; } = default!;
       
        // Alert
        //---------------------------------------------------------
        private bool blAlert = true;
        private ViqAlertComponent.AlertSeverity AlertSeverity = ViqAlertComponent.AlertSeverity.Info;
        private string AlertHeading = "Revenue Streams";
        private string AlertMessage = "Register all revenue streams, these will be assumed by your assessment reports and dashboards.";

        private List<IncomeType> incomeTypeList = new();
        private List<ZabDataTableAdvanced<IncomeType>.ColumnDefinition<IncomeType>> tableColumns = new();

        private ZabOffCanvas? canvasShell;
        private bool canvasOpenStatus = false;
        private string formTitle = "Income Type";
        private long activeRecordId = 0;
        private bool loadingStateActive = false;

        protected override async Task OnInitializedAsync()
        {
            _ = LoadGridDatasetAsync();

            tableColumns = new List<ZabDataTableAdvanced<IncomeType>.ColumnDefinition< IncomeType>>
            {
                new() { Title = "Income Type", Value = x => x.IncomeTypeName ?? "" },
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
                incomeTypeList = resultSet != null && resultSet.Any() ? resultSet.ToList() : new List< IncomeType>();
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


        private async Task DeleteSelectedSector( IncomeType targetModel)
        {
            var success = await sectorRepository.DeleteAsync(targetModel);
            if (success)
            {
                _Toast!.ShowSuccess("Income type removed.", sessionService!.AppTitle);
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

        private async Task ExecutePrintFormatProcess(List<IncomeType> targetedDataset)
        {
            try
            {
                loadingStateActive = true;
                StateHasChanged();

                var printDataSet = targetedDataset.Select(item => new ClientPrintDto
                {
                    IncomeTypeName    = item.IncomeTypeName,
                    Remarks = item.Remarks,
                    Status = item.Active == true ? "Active" : "Inactive"
                }).ToList();

                byte[] pdfBytes = await PdfService.GenerateReportDataPdfAsync(printDataSet, "Income Category List");
                string fileName = $"Income_Categories_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";

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

        private async Task ExecuteExcelExportProcess(List< IncomeType> targetedDataset)
        {
            try
            {
                loadingStateActive = true;
                byte[] excelBytes = await ExcelService.GenerateDataReportExcelAsync(targetedDataset, "Income Type Master Records");
                string fileName = $"Income_Category_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

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

        private async Task ExecuteEmailDistributionProcess(List< IncomeType> targetedDataset)
        {
            await Task.CompletedTask;
        }

        public class ClientPrintDto
        {
            [DisplayName("Income Type")]
            public string?IncomeTypeName { get; set; }
            public string? Remarks { get; set; }
            [DisplayName("Status")]
            public string? Status { get; set; }
        }
    }
}