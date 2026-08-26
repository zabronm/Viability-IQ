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
    public partial class AssessmentsPage : IAsyncDisposable
    {
        [Inject] private IGenericDataRepository<AssessmentDto> assessmentDtoRepository { get; set; } = default!;
        [Inject] private IGenericDataRepository<Assessment> coreAssessmentRepository { get; set; } = default!;
        [Inject] private NavigationManager Navigation { get; set; } = default!;
        [Inject] ISessionService? sessionService { get; set; }
        [Inject] ToastService? _Toast { get; set; }
        [Inject] OffCanvasStateService? OffcanvasService { get; set; } = default!;
        [Inject] private IJSRuntime JS { get; set; } = default!;
        [Inject] private IPdfExportService PdfService { get; set; } = default!;
        [Inject] private IExcelEPPlusExportService ExcelService { get; set; } = default!;

        private List<AssessmentDto> assessmentsList = new();
        private List<ZabDataTableAdvanced<AssessmentDto>.ColumnDefinition<AssessmentDto>> tableColumns = new();

        private bool loadingStateActive = false;

        protected override async Task OnInitializedAsync()
        {
            // ✅ Subscribe to OffCanvas callbacks
            OffcanvasService!.OnShow += HandleCanvasShow;

            _ = LoadGridDatasetAsync();

            tableColumns = new List<ZabDataTableAdvanced<AssessmentDto>.ColumnDefinition<AssessmentDto>>
            {
                // COLUMN 1: Case Number Link-Button
                new() {
                    Title = "Case Number",
                    CellTemplate = context => builder => {
                        var scopedAssessmentId = context.AssessmentId;
                        builder.OpenElement(0, "button");
                        builder.AddAttribute(1, "class", "btn btn-link p-0 fw-bold text-primary text-decoration-none link-underline-hover border-0 bg-transparent text-start");
                        builder.AddAttribute(2, "style", "font-size: inherit;");
                        builder.AddAttribute(3, "onclick", EventCallback.Factory.Create(this, () => InitializeAndRedirectToSessionAsync(scopedAssessmentId)));
                        builder.AddContent(4, context.CaseNumber);
                        builder.CloseElement();
                    }
                },
                new() { Title = "Case Type", Value = x => x.AssessmentTypeName ?? "" },
                
                // COLUMN 2: Business Name Link-Button
                new() {
                    Title = "Business Name",
                    CellTemplate = context => builder => {
                        var scopedBusinessId = context.BusinessId;
                        builder.OpenElement(0, "button");
                        builder.AddAttribute(1, "class", "btn btn-link p-0 text-primary text-decoration-none link-underline-hover border-0 bg-transparent text-start");
                        builder.AddAttribute(2, "style", "font-size: inherit;");
                        builder.AddAttribute(3, "onclick", EventCallback.Factory.Create(this, () => OpenBusinessDrawerForm(scopedBusinessId)));
                        builder.AddContent(4, context.BusinessName);
                        builder.CloseElement();
                    }
                },
                
                // COLUMN 3: Business Owner Link-Button
                new() {
                    Title = "Business Owner",
                    CellTemplate = context => builder => {
                        var scopedClientId = context.ClientId;
                        builder.OpenElement(0, "button");
                        builder.AddAttribute(1, "class", "btn btn-link p-0 text-primary text-decoration-none link-underline-hover border-0 bg-transparent text-start");
                        builder.AddAttribute(2, "style", "font-size: inherit;");
                        builder.AddAttribute(3, "onclick", EventCallback.Factory.Create(this, () => OpenClientDrawerForm(scopedClientId)));
                        builder.AddContent(4, context.BusinessOwner);
                        builder.CloseElement();
                    }
                },
                new() { Title = "Start Date", Value = x => x.AssessmentStartDate.ToString("yyyy-MM-dd") },
                new() { Title = "End Date", Value = x => x.AssessmentFinishDate.ToString("yyyy-MM-dd") },
                new() {
                    Title = "Status",
                    Value = x => GetStatusText(x.StatusId),
                    UseBadge = true,
                    BadgeClass = x => GetStatusBadgeClass(x.StatusId)
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
                var resultSet = await assessmentDtoRepository.GetAllAsync();
                assessmentsList = resultSet != null && resultSet.Any() ? resultSet.ToList() : new List<AssessmentDto>();
            }
            finally
            {
                loadingStateActive = false;
                StateHasChanged();
            }
        }

        private async Task InitializeAndRedirectToSessionAsync(long assessmentId)
        {
            var selectedRecord = assessmentsList.FirstOrDefault(x => x.AssessmentId == assessmentId);
            if (selectedRecord == null) return;

            try
            {
                if (sessionService != null)
                {
                    sessionService.SetActiveAssessment(
                         caseNumber: selectedRecord.CaseNumber,
                         assessmentId: selectedRecord.AssessmentId,
                         businessId: selectedRecord.BusinessId,
                         businessName: selectedRecord.BusinessName,
                         clientId: selectedRecord.ClientId,
                         clientName: selectedRecord.BusinessOwner,
                         assessmentType: selectedRecord.AssessmentTypeName,
                         HasExpensesData: true,
                         HasSalesData: true,
                         HasStockData: false,
                         HasReportsData: false,
                         HasReviewsData: false,
                         HasReviews: false,
                         HasDebtorsCreditorsData: true,
                         HasLoansData: true
                    );
                }

                Navigation.NavigateTo($"/assessment/dashboards/{selectedRecord.AssessmentId}");
            }
            catch (Exception ex)
            {
                _Toast!.ShowError($"Workspace redirection failed: {ex.Message}", "Routing Error");
            }
        }

        // ✅ Open Business form via service
        private async Task OpenBusinessDrawerForm(long businessId)
        {
            if (businessId == 0) return;

            await OffcanvasService!.ShowAsync(new CanvasRequest
            {
                Title = "Business Registry Summary View",
                Width = 500,
                ComponentType = typeof(BusinessFormComponent),
                Parameters = new Dictionary<string, object>
                {
                    { "BusinessId", businessId }
                },
                ResultCallback = async (result) => await ProcessExecutionFeedback(result, 2)
            });
        }

        // ✅ Open Client form via service
        private async Task OpenClientDrawerForm(long clientId)
        {
            if (clientId == 0) return;

            await OffcanvasService!.ShowAsync(new CanvasRequest
            {
                Title = "Client Profile Detail File",
                Width = 500,
                ComponentType = typeof(ClientFormComponent),
                Parameters = new Dictionary<string, object>
                {
                    { "ClientId", clientId }
                },
                ResultCallback = async (result) => await ProcessExecutionFeedback(result, 3)
            });
        }

        // ✅ Open Assessment form via service
        private async Task HandleFormExecution(long extractedRecordId)
        {
            string formTitle = extractedRecordId == 0 ? "Initiate New Assessment Case" : "Modify Assessment Settings";

            await OffcanvasService!.ShowAsync(new CanvasRequest
            {
                Title = formTitle,
                Width = 400,
                ComponentType = typeof(AssessmentsFormComponent),
                Parameters = new Dictionary<string, object>
                {
                    { "AssessmentId", extractedRecordId }
                },
                ResultCallback = async (result) => await ProcessExecutionFeedback(result, 1)
            });
        }

        private async Task DeleteSelectedAssessment(AssessmentDto targetDto)
        {
            var trackingPayload = new Assessment { AssessmentId = targetDto.AssessmentId };
            var success = await coreAssessmentRepository.DeleteAsync(trackingPayload);
            if (success)
            {
                _Toast!.ShowSuccess("Assessment file has been deleted from system tracking.", sessionService!.AppTitle);
                await LoadGridDatasetAsync();
            }
        }

        // ✅ Called when form completes
        private async Task ProcessExecutionFeedback(SaveResult _result, int panelIndex = 1)
        {
            if (_result.Success)
            {
                _Toast!.ShowSuccess(_result.Message, sessionService!.AppTitle);
                await LoadGridDatasetAsync();
            }
            else
            {
                _Toast!.ShowError(_result.Message, sessionService!.AppTitle);
            }

            StateHasChanged();
        }

        private string GetStatusText(long statusId) => statusId switch
        {
            1 => "Draft/Setup",
            2 => "In Progress",
            3 => "Under Review",
            4 => "Approved",
            5 => "Closed/Archived",
            _ => "Unknown State"
        };

        private string GetStatusBadgeClass(long statusId) => statusId switch
        {
            1 => "bg-secondary text-white small",
            2 => "bg-info text-black small",
            3 => "bg-warning text-black small",
            4 => "bg-success text-black small",
            5 => "bg-danger text-black small",
            _ => "bg-light text-black small",
        };

        private async Task ExecutePrintFormatProcess(List<AssessmentDto> targetedDataset)
        {
            try
            {
                loadingStateActive = true;
                byte[] pdfBytes = await PdfService.GenerateReportDataPdfAsync(targetedDataset, "Master Assessment Registry Ledger");
                string fileName = $"Assessments_Ledger_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
                await JS.InvokeVoidAsync("ZabFileSaver.DownloadBinaryStream", fileName, Convert.ToBase64String(pdfBytes));
            }
            finally
            {
                loadingStateActive = false;
                StateHasChanged();
            }
        }

        private async Task ExecuteExcelExportProcess(List<AssessmentDto> targetedDataset)
        {
            try
            {
                loadingStateActive = true;
                byte[] excelBytes = await ExcelService.GenerateDataReportExcelAsync(targetedDataset, "Assessments Output Sheet");
                string fileName = $"Assessments_Registry_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
                await JS.InvokeVoidAsync("ZabFileSaver.DownloadBinaryStream", fileName, Convert.ToBase64String(excelBytes));
            }
            finally
            {
                loadingStateActive = false;
                StateHasChanged();
            }
        }

        private async Task ExecuteEmailDistributionProcess(List<AssessmentDto> targetedDataset) => await Task.CompletedTask;

        // ✅ Cleanup subscriptions
        async ValueTask IAsyncDisposable.DisposeAsync()
        {
            if (OffcanvasService != null)
            {
                OffcanvasService.OnShow -= HandleCanvasShow;
            }
        }
    }
}