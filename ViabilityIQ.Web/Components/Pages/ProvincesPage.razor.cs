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
    public partial class ProvincesPage
    {
        [Inject] private IGenericDataRepository<Province> ProvinceRepository { get; set; } = default!;
        [Inject] ISessionService? sessionService { get; set; }
        [Inject] ToastService? _Toast { get; set; }
        [Inject] private OffCanvasStateService OffcanvasService { get; set; } = default!;
        [Inject] private IJSRuntime JS { get; set; } = default!;
        [Inject] private IPdfExportService PdfService { get; set; } = default!;
        [Inject] private IExcelEPPlusExportService ExcelService { get; set; } = default!;

        private List<Province> provincesList = new();
        private List<ZabDataTableAdvanced<Province>.ColumnDefinition<Province>> tableColumns = new();
        private bool loadingStateActive = false;

        protected override async Task OnInitializedAsync()
        {
            _ = LoadGridDatasetAsync();

            tableColumns = new List<ZabDataTableAdvanced<Province>.ColumnDefinition<Province>>
            {
                new() { Title = "Region Name", Value = x => x.ProvinceName },
                new() { Title = "Short Name", Value = x => x.ShortName ?? "" },
                new() { Title = "Manager User ID", Value = x => x.Manager_UserId == 0 ? "" : x.Manager_UserId.ToString() },
                new() { Title = "Telephone", Value = x => x.Telephone ?? "" },
                new() { Title = "Mobile", Value = x => x.Mobile ?? "" },
                new() { Title = "Email", Value = x => x.Email ?? "" },
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
                var resultSet = await ProvinceRepository.GetAllAsync();
                provincesList = resultSet != null && resultSet.Any() ? resultSet.ToList() : new List<Province>();
            }
            finally
            {
                loadingStateActive = false;
                StateHasChanged();
            }
        }

        private async Task HandleFormExecution(long extractedRecordId)
        {
            var formTitle = extractedRecordId == 0 ? "Add New Region" : "Modify Region Details";

            await OffcanvasService.ShowAsync(new CanvasRequest
            {
                Title = formTitle,
                Width = 500,
                ComponentType = typeof(ProvinceFormComponent),
                Parameters = new Dictionary<string, object>
                {
                    { "ProvinceId", extractedRecordId }
                },
                ResultCallback = ProcessExecutionFeedback
            });
        }

        private async Task DeleteSelectedProvince(Province targetProvince)
        {
            var success = await ProvinceRepository.DeleteAsync(targetProvince);
            if (success)
            {
                _Toast!.ShowSuccess("Record discarded successfully.", sessionService!.AppTitle);
                await LoadGridDatasetAsync();
            }
        }

        private async Task ProcessExecutionFeedback(SaveResult _result)
        {
            if (_result.Success)
            {
                _Toast!.ShowSuccess(_result.Message, sessionService!.AppTitle);
            }
            else
            {
                _Toast!.ShowError(_result.Message, sessionService!.AppTitle);
            }

            await LoadGridDatasetAsync();
            StateHasChanged();
        }

        private async Task ExecutePrintFormatProcess(List<Province> targetedDataset)
        {
            try
            {
                loadingStateActive = true;
                StateHasChanged();

                var PrintDataSet = targetedDataset.Select(item => new ProvinceListPrintDto
                {
                    ProvinceName = item.ProvinceName,
                    ShortName = item.ShortName,
                    Manager = item.Manager_UserId == 0 ? "" : item.Manager_UserId.ToString(),
                    Telephone = item.Telephone,
                    Email = item.Email,
                    Mobile = item.Mobile,
                    Status = item.Active
                }).ToList();

                byte[] pdfReportBytes = await PdfService.GenerateReportDataPdfAsync(PrintDataSet, "Regions List Summary");
                string targetFileName = $"Province_List_Summary_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";

                await JS.InvokeVoidAsync("ZabFileSaver.DownloadBinaryStream", targetFileName, Convert.ToBase64String(pdfReportBytes));
                _Toast!.ShowSuccess("PDF downloaded, check your downloads directory.", sessionService!.AppTitle);
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

        private async Task ExecuteExcelExportProcess(List<Province> targetedDataset)
        {
            try
            {
                loadingStateActive = true;

                byte[] excelBytes = await ExcelService.GenerateDataReportExcelAsync(targetedDataset, "Regions List Summary");
                string fileName = $"Province_List_Summary_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

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

        private async Task ExecuteEmailDistributionProcess(List<Province> targetedDataset)
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

        private class ProvinceListPrintDto
        {
            [DisplayName("Region Name")]
            public string? ProvinceName { get; set; }

            [DisplayName("Short Name")]
            public string? ShortName { get; set; }
            public string? Manager { get; set; }
            public string? Telephone { get; set; }
            public string? Mobile { get; set; }
            public string? Email { get; set; }
            public bool Status { get; set; }
        }
    }
}
