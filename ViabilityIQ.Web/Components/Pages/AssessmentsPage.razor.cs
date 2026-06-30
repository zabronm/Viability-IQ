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
    public partial class AssessmentsPage
    {
        [Inject] private IGenericDataRepository<AssessmentDto> assessmentDtoRepository { get; set; } = default!;
        [Inject] private IGenericDataRepository<Assessment> coreAssessmentRepository { get; set; } = default!;
        [Inject] private NavigationManager Navigation { get; set; } = default!;
        [Inject] ISessionService? sessionService { get; set; }
        [Inject] ToastService? _Toast { get; set; }
        [Inject] private IJSRuntime JS { get; set; } = default!;
        [Inject] private IPdfExportService PdfService { get; set; } = default!;
        [Inject] private IExcelEPPlusExportService ExcelService { get; set; } = default!;

        private List<AssessmentDto> assessmentsList = new();
        private List<ZabDataTableAdvanced<AssessmentDto>.ColumnDefinition<AssessmentDto>> tableColumns = new();

        // Canvas Drawer State Parameters
        private ZabOffCanvas? canvasShell;
        private bool canvasOpenStatus = false;
        private string formTitle = "Assessment";
        private long activeRecordId = 0;

        private ZabOffCanvas? businessCanvasShell;
        private bool businessCanvasOpen = false;
        private long activeBusinessId = 0;

        private ZabOffCanvas? clientCanvasShell;
        private bool clientCanvasOpen = false;
        private long activeClientId = 0;

        private bool loadingStateActive = false;

        protected override async Task OnInitializedAsync()
        {
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
                new() { Title = "Case Type", Value = x => x.AssessmentType ?? "" },
                
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
                //_Toast!.ShowInfo($"Configuring workspace environment for case {selectedRecord.CaseNumber}...", sessionService!.AppTitle);

                if (sessionService != null)
                {
                    sessionService.SetActiveAssessment(
                         caseNumber: selectedRecord.CaseNumber,
                         assessmentId: selectedRecord.AssessmentId,
                         businessId: selectedRecord.BusinessId,
                         businessName: selectedRecord.BusinessName,
                         clientId: selectedRecord.ClientId,
                         clientName: selectedRecord.BusinessOwner,
                         assessmentType: selectedRecord.AssessmentType,

                         HasExpensesData: true,        // selectedRecord.HasExpensesData,
                         HasSalesData: true,           // selectedRecord.HasSalesData,
                         HasStockData: false,           // selectedRecord.HasStockData,
                         HasReportsData: false,          // selectedRecord.HasReportsData,
                         HasReviewsData: false,           // selectedRecord.HasReviewsData,
                         HasReviews: false,
                         HasDebtorsCreditorsData: true,     //for testing only
                         HasLoansData: true                 //for testing only
                    );
                }

                Navigation.NavigateTo($"/assessment/dashboards/{selectedRecord.AssessmentId}");
            }
            catch (Exception ex)
            {
                _Toast!.ShowError($"Workspace redirection failed: {ex.Message}", "Routing Error");
            }
        }

        private async Task OpenBusinessDrawerForm(long businessId)
        {
            if (businessId == 0) return;

            // Assign identity context before modifying visibility parameters
            activeBusinessId = businessId;
            businessCanvasOpen = true;
            StateHasChanged();

            if (businessCanvasShell != null)
            {
                await businessCanvasShell.OpenAsync("Business Registry Summary View");
                StateHasChanged();
            }
        }

        private async Task OpenClientDrawerForm(long clientId)
        {
            if (clientId == 0) return;

            // Assign identity context before modifying visibility parameters
            activeClientId = clientId;
            clientCanvasOpen = true;
            StateHasChanged();

            if (clientCanvasShell != null)
            {
                await clientCanvasShell.OpenAsync("Client Profile Detail File");
                StateHasChanged();
            }
        }

        private async Task HandleFormExecution(long extractedRecordId)
        {
            activeRecordId = extractedRecordId;
            formTitle = extractedRecordId == 0 ? "Initiate New Assessment Case" : "Modify Assessment Settings";
            canvasOpenStatus = true;
            StateHasChanged();

            if (canvasShell != null)
            {
                await canvasShell.OpenAsync(formTitle);
            }
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

        private async Task ProcessExecutionFeedback(SaveResult _result, int panelIndex = 1)
        {
            if (_result.Success)
            {
                _Toast!.ShowSuccess(_result.Message, sessionService!.AppTitle);
            }

            if (_result.ClosePanel)
            {
                if (panelIndex == 1 && canvasShell != null) await canvasShell.CloseAsync();
                if (panelIndex == 2 && businessCanvasShell != null) await businessCanvasShell.CloseAsync();
                if (panelIndex == 3 && clientCanvasShell != null) await clientCanvasShell.CloseAsync();
            }

            await LoadGridDatasetAsync();
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
            //1 => "badge-secondary",
            //2 => "badge-info",
            //3 => "badge-warning",
            //4 => "badge-approved",
            //5 => "badge-rejected",
            //_ => "badge-light"
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
    }
}