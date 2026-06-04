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

        private ZabOffCanvas? canvasShell;
        private bool canvasOpenStatus = false;
        private string formTitle = "Assessment";
        private long activeRecordId = 0;
        private bool loadingStateActive = false;

        protected override async Task OnInitializedAsync()
        {
            _ = LoadGridDatasetAsync();

            // FIXED: Removed the non-existent 'CellTemplate' property assignment to resolve compiler error.
            tableColumns = new List<ZabDataTableAdvanced<AssessmentDto>.ColumnDefinition<AssessmentDto>>
            {
                new() { Title = "Case Number", Value = x => x.CaseNumber ?? "" },
                new() { Title = "Case Type", Value = x => x.AssessmentType },
                new() { Title = "Business Name", Value = x => x.BusinessName ?? "" },
                new() { Title = "Business Owner", Value = x => x.BusinessOwner ?? "" },
                new() { Title = "Start Date", Value = x => x.AssessmentStartDate.ToString("yyyy-MM-dd") ?? "" },
                new() { Title = "End Date", Value = x => x.AssessmentFinishDate.ToString("yyyy-MM-dd") ?? "" },
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

        /// <summary>
        /// This intercepts the record activation event, sets session variables, and moves to the workspace cockpit.
        /// </summary>
        private async Task InitializeAndRedirectToSessionAsync(long assessmentId)
        {
            var selectedRecord = assessmentsList.FirstOrDefault(x => x.AssessmentId == assessmentId);
            if (selectedRecord == null) return;

            try
            {
                _Toast!.ShowInfo($"Initializing session for case {selectedRecord.CaseNumber}...", sessionService!.AppTitle);

                // i) Initialize session state values
                if (sessionService != null)
                {
                    sessionService.SetActiveAssessment(
                         caseNumber: selectedRecord.CaseNumber,
                         assessmentId: selectedRecord.AssessmentId,
                         businessId: selectedRecord.BusinessId,
                         businessName: selectedRecord.BusinessName,
                         clientId: selectedRecord.ClientId,
                         clientName: selectedRecord.BusinessOwner,
                         assessmentType: selectedRecord.AssessmentType
                         );                                         
                                        
                }

                // ii) Redirect down to the diagnostic execution assessment page
                Navigation.NavigateTo($"/assessments/{selectedRecord.AssessmentId}");
            }
            catch (Exception ex)
            {
                _Toast!.ShowError($"Session initialization failed: {ex.Message}", "Routing Error");
            }
        }

        private async Task HandleFormExecution(long extractedRecordId)
        {
            activeRecordId = extractedRecordId;
            formTitle = extractedRecordId == 0 ? "Initiate New Assessment Case" : "Modify Assessment Settings";

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

        private async Task ProcessExecutionFeedback(SaveResult _result)
        {
            if (_result.Success)
            {
                _Toast!.ShowSuccess(_result.Message, sessionService!.AppTitle);
            }
            else
            {
                _Toast!.ShowError(_result.Message, "Operational Fault");
            }

            if (_result.ClosePanel && canvasShell != null)
            {
                await canvasShell.CloseAsync();
            }

            await LoadGridDatasetAsync();
            StateHasChanged();
        }

        private string GetStatusText(long statusId) => statusId switch
        {
            1 => "Draft / Setup",
            2 => "In Progress",
            3 => "Under Review",
            4 => "Approved",
            5 => "Closed / Archived",
            _ => "Unknown State"
        };

        private string GetStatusBadgeClass(long statusId) => statusId switch
        {
            1 => "badge-secondary",
            2 => "badge-info",
            3 => "badge-warning",
            4 => "badge-approved",
            5 => "badge-rejected",
            _ => "badge-light"
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