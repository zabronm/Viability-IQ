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
    public partial class ProvincesPage
    {
        [Inject] IReadOnlyRepository<ProvinceDto, long>? ProvinceRepository { get; set; }
        [Inject] private IGenericDataRepository<Province> clientGenRepository { get; set; } = default!;
        [Inject] ISessionService? sessionService { get; set; }
        [Inject] ToastService? _Toast { get; set; }
        [Inject] private IJSRuntime JS { get; set; } = default!;
        [Inject] private IPdfExportService PdfService { get; set; } = default!;
        [Inject] private IExcelEPPlusExportService ExcelService { get; set; } = default!;

        private List<ProvinceDto> provincesList = new();
        private List<ZabDataTableAdvanced<ProvinceDto>.ColumnDefinition<ProvinceDto>> tableColumns = new();

        private ZabOffCanvas? canvasShell;
        private bool canvasOpenStatus = false;
        private string formTitle = "Manage Province Account Record";
        private long activeRecordId = 0;
        private bool loadingStateActive = false;

        protected override async Task OnInitializedAsync()
        {
            _ = LoadGridDatasetAsync();

            // ALIGNED: Corrected property mappings for column expressions
            tableColumns = new List<ZabDataTableAdvanced<ProvinceDto>.ColumnDefinition<ProvinceDto>>
            {
                new() { Title = "Province Name", Value = x => x.ProvinceName },
                new() { Title = "Short Name", Value = x => x.ShortName ?? "" },
                new() { Title = "Manager", Value = x => x.Manager ?? "" },
                new() { Title = "Telephone", Value = x => x.Telephone ?? "" },
                new() { Title = "Mobile", Value = x => x.Mobile ?? "" }, // Maps clean description or tracking ID               
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
                var resultSet = await ProvinceRepository!.GetAllAsync();
                provincesList = resultSet != null && resultSet.Any() ? resultSet.ToList() : new List<ProvinceDto>();
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
            formTitle = extractedRecordId == 0 ? "Add New Province/Funder" : "Modify Province Details";

            if (canvasShell != null)
            {
                await canvasShell.OpenAsync(formTitle);
            }
        }

        // ALIGNED: Unified deletion input to use ProvinceDto matching the TItem grid specifier type safely
        private async Task DeleteSelectedProvince(ProvinceDto targetProvinceDto)
        {
            // Map DTO to domain model context schema block for generic repo execution target
            var targetProvince = new Province
            {
                ProvinceId = targetProvinceDto.ProvinceId,
                ProvinceName = targetProvinceDto.ProvinceName
            };

            var success = await clientGenRepository!.DeleteAsync(targetProvince);
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

        // PDF EXPORT EXECUTOR ALIGNED (Takes List<ProvinceDto> from component handler pipeline now)
        private async Task ExecutePrintFormatProcess(List<ProvinceDto> targetedDataset)
        {
            try
            {
                loadingStateActive = true;
                StateHasChanged();

                var PrintDataSet = targetedDataset.Select(item => new provinceListPrintDto
                {
                    ProvinceName = item.ProvinceName,
                    ShortName = item.ShortName,
                    Manager = item.Manager,
                    Telephone = item.Telephone,
                    Email = item.Email,
                    Mobile = item.Mobile,
                    Status = item.Active
                }).ToList();

                byte[] pdfReportBytes = await PdfService.GenerateReportDataPdfAsync(PrintDataSet, "Provinces List Summary");
                string targetFileName = $"Province_List_Summary_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";

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
        private async Task ExecuteExcelExportProcess(List<ProvinceDto> targetedDataset)
        {
            try
            {
                loadingStateActive = true;

                byte[] excelBytes = await ExcelService.GenerateDataReportExcelAsync(targetedDataset, "Provinces List Summary");
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

        // EMAIL DISTRIBUTION EXECUTOR ALIGNED
        private async Task ExecuteEmailDistributionProcess(List<ProvinceDto> targetedDataset)
        {
            try
            {
                // Uncoment and add your EmailService configuration calls mapping ProvinceDto records 
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

        private class provinceListPrintDto
        {
            [DisplayName("Province Name")]
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