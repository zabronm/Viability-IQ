using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System.ComponentModel;
using System.Data;
using System.Security.Cryptography.Pkcs;
using ViabilityIQ.Application.Dtos;
using ViabilityIQ.Application.Interfaces;
using ViabilityIQ.Infrastructure.Repositories;
using ViabilityIQ.Shared.DataModels;
using ViabilityIQ.Shared.SharedModels;
using ViabilityIQ.Web.Components.CommonComponents;
using ViabilityIQ.Web.Components.Pages.PageFormComponents;
using ViabilityIQ.Web.Services;
using static Microsoft.Data.SqlClient.Internal.SqlClientEventSource;

namespace ViabilityIQ.Web.Components.Pages
{
    public partial class CompanyPage : IAsyncDisposable
    {
        [Inject] private IGenericDataRepository<Company> companyRepository { get; set; } = default!;
        [Inject] ISessionService? sessionService { get; set; }
        [Inject] ToastService? _Toast { get; set; }
        [Inject] OffCanvasStateService? OffcanvasService { get; set; } = default!;  // ✅ ADD THIS
        [Inject] private IJSRuntime JS { get; set; } = default!;
        [Inject] private IPdfExportService PdfService { get; set; } = default!;
        [Inject] private IExcelEPPlusExportService ExcelService { get; set; } = default!;

        //---------------------------------------------------------
        // Alert
        //---------------------------------------------------------
        private bool blAlert = true;
        private ViqAlertComponent.AlertSeverity AlertSeverity = ViqAlertComponent.AlertSeverity.Info;
        private string AlertHeading = "Company Register";
        private string AlertMessage = "Register your company details here. All users will be registered under your company tenant; All reports will be based on information from this section.";

        private List<Company> companyList = new();
        private List<ZabDataTableAdvanced<Company>.ColumnDefinition<Company>> tableColumns = new();

        private bool loadingStateActive = false;

        protected override async Task OnInitializedAsync()
        {
            // ✅ Subscribe to OffCanvas callbacks
            OffcanvasService!.OnShow += HandleCanvasShow;

            _ = LoadGridDatasetAsync();

            tableColumns = new List<ZabDataTableAdvanced<Company>.ColumnDefinition<Company>>
            {
                new() { Title = "Company Name", Value = x => x.CompanyName ?? "" },
                new() { Title = "Contact", Value = x => x.ContactPerson ?? "" },
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
                var resultSet = await companyRepository.GetAllAsync();
                companyList = resultSet != null && resultSet.Any() ? resultSet.ToList() : new List<Company>();
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
            string formTitle = extractedRecordId == 0 ? "Add Company Details" : "Modify Company Details";

            await OffcanvasService!.ShowAsync(new CanvasRequest
            {
                Title = formTitle,
                Width = 500,
                ComponentType = typeof(CompanyFormComponent),
                Parameters = new Dictionary<string, object>
                {
                    { "CompanyId", extractedRecordId }
                },
                ResultCallback = async (result) => await ProcessExecutionFeedback(result)
            });
        }

        private async Task DeleteSelectedCompany(Company targetCompany)
        {
            var success = await companyRepository!.DeleteAsync(targetCompany);
            if (success)
            {
                _Toast!.ShowSuccess("Company record deleted successfully.", sessionService!.AppTitle);
                await LoadGridDatasetAsync();
            }
            else
            {
                _Toast!.ShowError("Failed to delete company record.", sessionService!.AppTitle);
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

        private async Task ExecutePrintFormatProcess(List<Company> targetedDataset)
        {
            try
            {
                loadingStateActive = true;
                StateHasChanged();

                var PrintDataSet = targetedDataset.Select(item => new companyPrintDto
                {
                    CustomerCode = item.CompanyCustomerCode,
                    CompanyName = item.CompanyName,
                    Contact = item.ContactPerson,
                    Telephone = item.Telephone,
                    Mobile = item.Mobile,
                    Email = item.Email,
                    Active = item.Active,
                }).ToList();

                byte[] pdfReportBytes = await PdfService.GenerateReportDataPdfAsync(PrintDataSet, "Company Summary");
                string targetFileName = $"Company_Register_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";

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

        private async Task ExecuteExcelExportProcess(List<Company> targetedDataset)
        {
            try
            {
                loadingStateActive = true;

                byte[] excelBytes = await ExcelService.GenerateDataReportExcelAsync(targetedDataset, "Company Register");
                string fileName = $"Company_Register_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

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

        private async Task ExecuteEmailDistributionProcess(List<Company> targetedDataset)
        {
            try
            {
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _Toast!.ShowError($"Email Transmission Engine Intercepted Error: {ex.Message}", sessionService!.AppTitle);
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

        public class companyPrintDto
        {
            [DisplayName("Company Name")]
            public string? CompanyName { get; set; }

            [DisplayName("Co. Code")]
            public string? CustomerCode { get; set; }
            public string? Contact { get; set; }
            public string? Telephone { get; set; }
            public string? Mobile { get; set; }
            public string? Email { get; set; }
            public bool Active { get; set; }
        }
    }
}