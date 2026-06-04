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
    public partial class ClientPage
    {
        [Inject] IReadOnlyRepository<ClientDto, long>? ClientRepository { get; set; }
        [Inject] private IGenericDataRepository<Client> clientGenRepository { get; set; } = default!;
        [Inject] ISessionService? sessionService { get; set; }
        [Inject] ToastService? _Toast { get; set; }
        [Inject] private IJSRuntime JS { get; set; } = default!;
        [Inject] private IPdfExportService PdfService { get; set; } = default!;
        [Inject] private IExcelEPPlusExportService ExcelService { get; set; } = default!;

        private List<ClientDto> clientsList = new();
        private List<ZabDataTableAdvanced<ClientDto>.ColumnDefinition<ClientDto>> tableColumns = new();

        private ZabOffCanvas? canvasShell;
        private bool canvasOpenStatus = false;
        private string formTitle = "Manage Client Account Record";
        private long activeRecordId = 0;
        private bool loadingStateActive = false;

        protected override async Task OnInitializedAsync()
        {
            _ = LoadGridDatasetAsync();

            // ALIGNED: Corrected property mappings for column expressions
            tableColumns = new List<ZabDataTableAdvanced<ClientDto>.ColumnDefinition<ClientDto>>
            {
                new() { Title = "Client Name", Value = x => x.ClientName },
                new() { Title = "Type", Value = x => x.ClientType ?? "" },
                new() { Title = "Gender", Value = x => x.Gender ?? "" },
                new() { Title = "Class", Value = x => x.Race ?? "" },
                new() { Title = "Province", Value = x => x.ProvinceName ?? "" }, // Maps clean description or tracking ID
                new() { Title = "Mobile", Value = x => x.Mobile ?? "" },     // Adjusted column tracking
                new() { Title = "Email", Value = x => x.Email ?? "" },
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
                var resultSet = await ClientRepository!.GetAllAsync();
                clientsList = resultSet != null && resultSet.Any() ? resultSet.ToList() : new List<ClientDto>();
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
            formTitle = extractedRecordId == 0 ? "Add New Client/Funder" : "Modify Client Details";

            if (canvasShell != null)
            {
                await canvasShell.OpenAsync(formTitle);
            }
        }

        // ALIGNED: Unified deletion input to use ClientDto matching the TItem grid specifier type safely
        private async Task DeleteSelectedClient(ClientDto targetClientDto)
        {
            // Map DTO to domain model context schema block for generic repo execution target
            var targetClient = new Client
            {
                ClientId = targetClientDto.ClientId,
                ClientName = targetClientDto.ClientName
            };

            var success = await clientGenRepository!.DeleteAsync(targetClient);
            if (success)
            {
                _Toast!.ShowSuccess("Record discarded successfully.", sessionService!.AppTitle);
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
                _Toast!.ShowError(_result.Message, sessionService!.AppTitle);
            }

            if (_result.ClosePanel && canvasShell != null)
            {
                await canvasShell.CloseAsync();
            }

            await LoadGridDatasetAsync();
            StateHasChanged();
        }

        // PDF EXPORT EXECUTOR ALIGNED (Takes List<ClientDto> from component handler pipeline now)
        private async Task ExecutePrintFormatProcess(List<ClientDto> targetedDataset)
        {
            try
            {
                loadingStateActive = true;
                StateHasChanged();

                var PrintDataSet = targetedDataset.Select(item => new clientListPrintDto
                {
                    ClientName = item.ClientName,
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

        // EXCEL EXPORT EXECUTOR ALIGNED
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

        // EMAIL DISTRIBUTION EXECUTOR ALIGNED
        private async Task ExecuteEmailDistributionProcess(List<ClientDto> targetedDataset)
        {
            try
            {
                // Uncoment and add your EmailService configuration calls mapping ClientDto records 
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