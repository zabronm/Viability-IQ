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
    public partial class LoanTypesPage : IAsyncDisposable
    {
        [Inject] private IGenericDataRepository<LoanType> LoanTypeRepository { get; set; } = default!;
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
        private string AlertHeading = "Loan Types";
        private string AlertMessage = "Register types of typical loans here. Each loan type will have different implications like interest rates and regulations.";

        private List<LoanType> loanTypesList = new();
        private List<ZabDataTableAdvanced<LoanType>.ColumnDefinition<LoanType>> tableColumns = new();

        private bool loadingStateActive = false;

        protected override async Task OnInitializedAsync()
        {
            // ✅ Subscribe to OffCanvas callbacks
            OffcanvasService!.OnShow += HandleCanvasShow;

            _ = LoadGridDatasetAsync();

            tableColumns = new List<ZabDataTableAdvanced<LoanType>.ColumnDefinition<LoanType>>
            {
                new() { Title = "Loan Type Name", Value = x => x.LoanTypeName },
                new() { Title = "Other Name/s", Value = x => x.ShortName },
                new() { Title = "Remarks/Details", Value = x => x.Remarks ?? "N/A" },
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
                var resultSet = await LoanTypeRepository.GetAllAsync();
                loanTypesList = resultSet != null && resultSet.Any() ? resultSet.ToList() : new List<LoanType>();
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
            string formTitle = extractedRecordId == 0 ? "Add New Loan Type" : "Modify Loan Type Details";

            await OffcanvasService!.ShowAsync(new CanvasRequest
            {
                Title = formTitle,
                Width = 400,
                ComponentType = typeof(LoanTypeFormComponent),
                Parameters = new Dictionary<string, object>
                {
                    { "LoanTypeId", extractedRecordId }
                },
                ResultCallback = async (result) => await ProcessExecutionFeedback(result)
            });
        }

        private async Task DeleteSelectedLoanType(LoanType targetLoanType)
        {
            var success = await LoanTypeRepository.DeleteAsync(targetLoanType);
            if (success)
            {
                _Toast!.ShowSuccess("Loan type removed successfully.", sessionService!.AppTitle);
                await LoadGridDatasetAsync();
            }
            else
            {
                _Toast!.ShowError("Failed to delete loan type.", sessionService!.AppTitle);
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

        private async Task ExecutePrintFormatProcess(List<LoanType> targetedDataset)
        {
            try
            {
                loadingStateActive = true;
                StateHasChanged();

                var PrintDataSet = targetedDataset.Select(item => new LoanTypePrintDto
                {
                    LoanTypeName = item.LoanTypeName,
                    ShortName = item.ShortName,
                    Active = item.Active,
                    Remarks = item.Remarks
                }).ToList();

                byte[] pdfReportBytes = await PdfService.GenerateReportDataPdfAsync(PrintDataSet, "Registered Loan Types Ledger Summary");
                string targetFileName = $"Loan_Type_Master_Ledger_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";

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

        private async Task ExecuteExcelExportProcess(List<LoanType> targetedDataset)
        {
            try
            {
                loadingStateActive = true;

                byte[] excelBytes = await ExcelService.GenerateDataReportExcelAsync(targetedDataset, "Registered Loan Type List");
                string fileName = $"Loan_Type_Export_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

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

        private async Task ExecuteEmailDistributionProcess(List<LoanType> targetedDataset)
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

        public class LoanTypePrintDto
        {
            [DisplayName("Loan Type Name")]
            public string? LoanTypeName { get; set; }
            [DisplayName("Other Name/s")]
            public string? ShortName { get; set; }
            public string? Remarks { get; set; }
            [DisplayName("Status")]
            public bool Active { get; set; }
        }
    }
}