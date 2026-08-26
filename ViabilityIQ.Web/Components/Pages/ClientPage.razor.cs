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
    public partial class ClientPage : IAsyncDisposable
    {
        [Inject] IReadOnlyRepository<ClientDto, long>? ClientRepository { get; set; }
        [Inject] private IGenericDataRepository<Client> clientGenRepository { get; set; } = default!;
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
        private string AlertHeading = "Client Register";
        private string AlertMessage = "Register clients who will own businesses in your assessments. Provide as accurate and detailed data as possible to ensure more accurate statistics and projections.";

        private List<ClientDto> clientsList = new();
        private List<ZabDataTableAdvanced<ClientDto>.ColumnDefinition<ClientDto>> tableColumns = new();

        private bool loadingStateActive = false;

        protected override async Task OnInitializedAsync()
        {
            // ✅ Subscribe to OffCanvas callbacks
            OffcanvasService!.OnShow += HandleCanvasShow;

            _ = LoadGridDatasetAsync();

            tableColumns = new List<ZabDataTableAdvanced<ClientDto>.ColumnDefinition<ClientDto>>
            {
                new() { Title = "Client Name", Value = x => x.Client },
                new() { Title = "Category", Value = x => x.ClientTypeName ?? "" },
                new() { Title = "Gender", Value = x => x.Gender ?? "" },
                new() { Title = "Class", Value = x => x.Race ?? "" },
                new() { Title = "Province", Value = x => x.ProvinceName ?? "" },
                new() { Title = "Mobile", Value = x => x.Mobile ?? "" },
                new() {
                    Title = "Status",
                    Value = x => x.Active == true ? "Active" : "Inactive",
                    UseBadge = true,
                    BadgeClass = x => x.Active == true ? "badge-approved" : "badge-rejected"
                }
            };
            await Task.CompletedTask;
        }

        // ✅ Handle when form completes (called by OffcanvasService)
        private async Task HandleCanvasShow(CanvasRequest request)
        {
            // Just acknowledge that the canvas opened
            await Task.CompletedTask;
        }

        private async Task LoadGridDatasetAsync()
        {
            loadingStateActive = true;
            StateHasChanged();

            try
            {
                var resultSet = await ClientRepository!.GetAllAsync();
                clientsList = resultSet != null && resultSet.Any() ? resultSet.ToList() : new List<ClientDto>();
            }
            finally
            {
                loadingStateActive = false;
                StateHasChanged();
            }
        }

        // ✅ This is called when Add/Edit button is clicked
        private async Task HandleFormExecution(long extractedRecordId)
        {
            string formTitle = extractedRecordId == 0 ? "Add New Client" : "Modify Client Details";

            // ✅ Show the OffCanvas through the service
            // The form will be rendered in MainLayout's ZabOffCanvas
            await OffcanvasService!.ShowAsync(new CanvasRequest
            {
                Title = formTitle,
                Width = 550,
                ComponentType = typeof(ClientFormComponent),
                Parameters = new Dictionary<string, object>
                {
                    { "ClientId", extractedRecordId }
                },
                // ✅ Set callback for when form completes
                ResultCallback = async (result) => await ProcessExecutionFeedback(result)
            });
        }

        private async Task DeleteSelectedClient(ClientDto targetClientDto)
        {
            var targetClient = new Client
            {
                ClientId = targetClientDto.ClientId,
                FullName = targetClientDto.Client
            };

            var success = await clientGenRepository!.DeleteAsync(targetClient);
            if (success)
            {
                _Toast!.ShowSuccess("Record discarded successfully.", sessionService!.AppTitle);
                await LoadGridDatasetAsync();
            }
        }

        // ✅ This is called when the form finishes (via OffcanvasService.PublishResultAsync)
        private async Task ProcessExecutionFeedback(SaveResult _result)
        {
            if (_result.Success)
            {
                _Toast!.ShowSuccess(_result.Message, sessionService!.AppTitle);
                await LoadGridDatasetAsync();  // ✅ Refresh the grid
            }
            else
            {
                _Toast!.ShowError(_result.Message, sessionService!.AppTitle);
            }

            StateHasChanged();
        }

        private async Task ExecutePrintFormatProcess(List<ClientDto> targetedDataset)
        {
            try
            {
                loadingStateActive = true;
                StateHasChanged();

                var PrintDataSet = targetedDataset.Select(item => new clientListPrintDto
                {
                    ClientName = item.Client,
                    Gender = item.Gender,
                    Race = item.Race,
                    Province = item.ProvinceName,
                    Mobile = item.Mobile,
                    Status = item.Active
                }).ToList();

                byte[] pdfReportBytes = await PdfService.GenerateReportDataPdfAsync(PrintDataSet, "Clients List Summary");
                string targetFileName = $"Client_List_Summary_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";

                await JS.InvokeVoidAsync("ZabFileSaver.DownloadBinaryStream", targetFileName, Convert.ToBase64String(pdfReportBytes));
                _Toast!.ShowSuccess("PDF downloaded, check your downloads directory..", sessionService!.AppTitle);
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

        private async Task ExecuteExcelExportProcess(List<ClientDto> targetedDataset)
        {
            try
            {
                loadingStateActive = true;

                byte[] excelBytes = await ExcelService.GenerateDataReportExcelAsync(targetedDataset, "Clients List Summary");
                string fileName = $"Client_List_Summary_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

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

        private async Task ExecuteEmailDistributionProcess(List<ClientDto> targetedDataset)
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

        private class clientListPrintDto
        {
            [DisplayName("Client Name")]
            public string? ClientName { get; set; }
            public string? IDNumber { get; set; }
            public string? Gender { get; set; }
            public string? Race { get; set; }
            public string? Mobile { get; set; }
            public string? Province { get; set; }
            public bool Status { get; set; }
        }
    }
}