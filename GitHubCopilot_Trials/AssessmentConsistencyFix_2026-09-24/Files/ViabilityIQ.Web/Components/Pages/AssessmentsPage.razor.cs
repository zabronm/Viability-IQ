using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using ViabilityIQ.Application.Dtos;
using ViabilityIQ.Application.Interfaces;
using ViabilityIQ.Application.Interfaces.HomePageInterfaces;
using ViabilityIQ.Application.Interfaces.IdentityInterfaces;
using ViabilityIQ.Shared.DataModels;
using ViabilityIQ.Shared.SharedModels;
using ViabilityIQ.Web.Components.CommonComponents;
using ViabilityIQ.Web.Components.Pages.PageFormComponents;
using ViabilityIQ.Web.Components.Pages_Assessments.AssumptionComponents;
using ViabilityIQ.Web.Models.Dashboard;
using ViabilityIQ.Web.Services;

namespace ViabilityIQ.Web.Components.Pages
{
    public partial class AssessmentsPage : IAsyncDisposable
    {
        [SupplyParameterFromQuery(Name = "status")] public long? StatusFilter { get; set; }
        [SupplyParameterFromQuery(Name = "readiness")] public int? ReadinessFilter { get; set; }
        [Inject] private IReadOnlyRepository<AssessmentDto, long> assessmentDtoRepository { get; set; } = default!;
        [Inject] private IGenericDataRepository<Assessment> coreAssessmentRepository { get; set; } = default!;
        [Inject] private IAssessmentDataValidationService dataValidationService { get; set; } = default!;
        [Inject] private IAssessmentReadinessService readinessService { get; set; } = default!;
        [Inject] private IDashboardDataService DashboardDataService { get; set; } = default!;
        [Inject] private AuthenticationStateProvider AuthenticationStateProvider { get; set; } = default!;
        [Inject] private IAuthenticationService AuthenticationService { get; set; } = default!;
        [Inject] private NavigationManager Navigation { get; set; } = default!;
        [Inject] ISessionService? sessionService { get; set; }
        [Inject] ToastService? _Toast { get; set; }
        [Inject] OffCanvasStateService? OffcanvasService { get; set; } = default!;
        [Inject] private IJSRuntime JS { get; set; } = default!;
        [Inject] private IPdfExportService PdfService { get; set; } = default!;
        [Inject] private IExcelEPPlusExportService ExcelService { get; set; } = default!;
        [Inject] private ILogger<AssessmentsPage> Logger { get; set; } = default!;

        private List<ZabDataTableAdvanced<AssessmentDto>.ColumnDefinition<AssessmentDto>> tableColumns = new();
        private ZabDataTableAdvanced<AssessmentDto>? assessmentTable;
        private ZabConfirmDialogComponent? LifecycleConfirmDialog;
        private KPIMetricsModel LifecycleMetrics { get; set; } = new();

        private bool loadingStateActive = false;

        protected override async Task OnInitializedAsync()
        {
            OffcanvasService!.OnShow += HandleCanvasShow;

            tableColumns = new List<ZabDataTableAdvanced<AssessmentDto>.ColumnDefinition<AssessmentDto>>
            {
                new() {
                    Title = "Case Number",
                    ServerField = nameof(AssessmentDto.CaseNumber),
                    Value = x => x.CaseNumber,
                    Filterable = true,
                    CellTemplate = context => builder => {
                        builder.OpenElement(0, "button");
                        builder.AddAttribute(1, "class", "btn btn-link p-0 fw-bold text-primary text-decoration-none link-underline-hover border-0 bg-transparent text-start");
                        builder.AddAttribute(2, "style", "font-size: inherit;");
                        builder.AddAttribute(3, "onclick", EventCallback.Factory.Create(this, () => InitializeAndRedirectToSessionAsync(context)));
                        builder.AddContent(4, context.CaseNumber);
                        builder.CloseElement();
                    }
                },
                new() {
                    Title = "Case Type", ServerField = nameof(AssessmentDto.AssessmentTypeName),
                    Value = x => x.AssessmentTypeName ?? "", Filterable = true
                },
                
                // COLUMN 2: Business Name Link-Button
                new() {
                    Title = "Business Name",
                    ServerField = nameof(AssessmentDto.BusinessName),
                    Value = x => x.BusinessName,
                    Filterable = true,
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
                    ServerField = nameof(AssessmentDto.BusinessOwner),
                    Value = x => x.BusinessOwner,
                    Filterable = true,
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
                new() {
                    Title = "Start Date", ServerField = nameof(AssessmentDto.AssessmentStartDate),
                    Value = x => x.AssessmentStartDate, FormatString = "yyyy-MM-dd", Searchable = false
                },
                new() {
                    Title = "End Date", ServerField = nameof(AssessmentDto.AssessmentFinishDate),
                    Value = x => x.AssessmentFinishDate, FormatString = "yyyy-MM-dd", Searchable = false
                },
                new() {
                    Title = "Status",
                    ServerField = nameof(AssessmentDto.StatusId),
                    Value = x => GetStatusText(x.StatusId),
                    Searchable = false,
                    UseBadge = true,
                    BadgeClass = x => GetStatusBadgeClass(x.StatusId)
                },
                new() {
                    Title = "Readiness & Actions",
                    Searchable = false,
                    CellTemplate = context => builder => {
                        builder.OpenComponent<AssessmentLifecycleActionsComponent>(0);
                        builder.AddAttribute(1, nameof(AssessmentLifecycleActionsComponent.Assessment), context);
                        builder.AddAttribute(2, nameof(AssessmentLifecycleActionsComponent.OnViewReadiness),
                            EventCallback.Factory.Create<AssessmentDto>(this, ViewReadinessAsync));
                        builder.AddAttribute(3, nameof(AssessmentLifecycleActionsComponent.OnComplete),
                            EventCallback.Factory.Create<AssessmentDto>(this, MarkCompleteAsync));
                        builder.AddAttribute(4, nameof(AssessmentLifecycleActionsComponent.OnReopen),
                            EventCallback.Factory.Create<AssessmentDto>(this, ReopenAsync));
                        builder.AddAttribute(5, nameof(AssessmentLifecycleActionsComponent.OnSubmitForReview),
                            EventCallback.Factory.Create<AssessmentDto>(this, SubmitForReviewAsync));
                        builder.CloseComponent();
                    }
                }
            };
            await RefreshLifecycleMetricsAsync();
        }

        // ✅ Handle when canvas opens
        private async Task HandleCanvasShow(CanvasRequest request)
        {
            await Task.CompletedTask;
        }

        private Task<DataTablePage<AssessmentDto>> LoadAssessmentPageAsync(
            DataTableQuery query,
            CancellationToken cancellationToken)
        {
            var filters = new Dictionary<string, string>(query.Filters, StringComparer.OrdinalIgnoreCase);
            if (StatusFilter.HasValue)
                filters[nameof(AssessmentDto.StatusId)] = StatusFilter.Value.ToString();
            if (ReadinessFilter.HasValue)
                filters[nameof(AssessmentDto.ProgressPercentage)] = ReadinessFilter.Value.ToString();

            return assessmentDtoRepository.GetPageAsync(new DataTableQuery
            {
                PageNumber = query.PageNumber,
                PageSize = query.PageSize,
                SearchText = query.SearchText,
                SearchFields = query.SearchFields,
                SortField = query.SortField,
                SortDescending = query.SortDescending,
                Filters = filters
            }, cancellationToken);
        }

        private Task HandleTableLoadError(Exception exception)
        {
            _Toast!.ShowError("Assessment data could not be loaded. Please retry.", sessionService!.AppTitle);
            return Task.CompletedTask;
        }

        // ✅ Initialize session and redirect to the assessment session page
        private async Task InitializeAndRedirectToSessionAsync(AssessmentDto selectedRecord)
        {
            try
            {
                if (sessionService == null)
                {
                    _Toast!.ShowError("Session service is not available.", "Error");
                    return;
                }

                // This queries the database to check which data types have records
                var dataStatus = await dataValidationService.ValidateAssessmentDataAsync(selectedRecord.AssessmentId);
                // Set the session with the ACTUAL data status (not hardcoded values)
                sessionService.SetActiveAssessment(
                    caseNumber: selectedRecord.CaseNumber,
                    assessmentId: selectedRecord.AssessmentId,
                    businessId: selectedRecord.BusinessId,
                    businessName: selectedRecord.BusinessName,
                    clientId: selectedRecord.ClientId,
                    clientName: selectedRecord.BusinessOwner,
                    assessmentType: selectedRecord.AssessmentTypeName,
                    HasAssetsData: dataStatus.HasAssets,              // ✅ FROM VALIDATION
                    HasExpensesData: dataStatus.HasExpenses,          // ✅ FROM VALIDATION
                    HasSalesData: dataStatus.HasSales,                // ✅ FROM VALIDATION
                    HasStockData: dataStatus.HasStock,                // ✅ FROM VALIDATION
                    HasReportsData: dataStatus.HasReports,            // ✅ FROM VALIDATION
                    HasReviewsData: dataStatus.HasReviews,            // ✅ FROM VALIDATION
                    HasReviews: dataStatus.HasReviews,                // ✅ FROM VALIDATION
                    HasDebtorsCreditorsData: dataStatus.HasDebtorsCreditors,  // ✅ FROM VALIDATION
                    HasLoansData: dataStatus.HasLoans                 // ✅ FROM VALIDATION
                );

                // ✅ STEP 4: LOG VALIDATION RESULTS FOR DEBUGGING
                System.Diagnostics.Debug.WriteLine($"[AssessmentsPage] {dataStatus.GetSummary()}");
                //System.Diagnostics.Debug.WriteLine($"[AssessmentsPage] Loaded modules: {string.Join(", ", dataStatus.LoadedDataTypes)}");
                //System.Diagnostics.Debug.WriteLine($"[AssessmentsPage] Missing modules: {string.Join(", ", dataStatus.MissingDataTypes)}");

                // ✅ STEP 5: NAVIGATE TO DASHBOARD
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
                if (assessmentTable is not null)
                    await assessmentTable.RefreshAsync();
            }
        }

        // ✅ Called when form completes
        private async Task ProcessExecutionFeedback(SaveResult _result, int panelIndex = 1)
        {
            if (_result.Success)
            {
                _Toast!.ShowSuccess(_result.Message, sessionService!.AppTitle);
                if (assessmentTable is not null)
                    await assessmentTable.RefreshAsync();
                await RefreshLifecycleMetricsAsync();
            }
            else if (!_result.Cancelled)
            {
                _Toast!.ShowError(_result.Message, sessionService!.AppTitle);
            }

            StateHasChanged();
        }

        private string GetStatusText(long statusId) => statusId switch
        {
            _ => AssessmentLifecycleStatus.GetName(statusId)
        };

        private string GetStatusBadgeClass(long statusId) => statusId switch
        {
            1 => "bg-secondary text-white small",
            2 => "bg-info text-black small",
            3 => "bg-warning text-dark small",
            4 => "bg-success text-white small",
            5 => "bg-danger text-black small",
            _ => "bg-light text-black small",
        };

        private async Task RefreshLifecycleMetricsAsync()
        {
            var userId = await ResolveAuthenticatedUserIdAsync();
            if (userId <= 0)
            {
                LifecycleMetrics = new KPIMetricsModel();
                Logger.LogWarning("Assessment lifecycle metrics were not loaded because the authenticated user could not be resolved.");
                return;
            }

            // Use the same KPI source as HomePage so both pages always apply identical
            // user scoping, active-record filtering, and status formulas.
            LifecycleMetrics = await DashboardDataService.GetKPIMetricsAsync(userId);
            await InvokeAsync(StateHasChanged);

            var assessments = (await coreAssessmentRepository.GetAllAsync())
                .Where(item => item.Active && item.CreatedBy == userId)
                .ToArray();
            var lifecycleChanged = false;

            foreach (var item in assessments)
            {
                try
                {
                    var readiness = await readinessService.EvaluateAsync(item.AssessmentId);
                    if (item.ProgressPercentage != readiness.Score
                        || item.StatusId == AssessmentLifecycleStatus.Draft && readiness.Score > 0)
                    {
                        item.ProgressPercentage = readiness.Score;
                        if (item.StatusId == AssessmentLifecycleStatus.Draft && readiness.Score > 0)
                        {
                            item.StatusId = AssessmentLifecycleStatus.InProgress;
                        }

                        item.ModifiedDate = DateTime.UtcNow;
                        item.ModifiedBy = userId;
                        await coreAssessmentRepository.SaveAsync(item);
                        lifecycleChanged = true;
                    }
                }
                catch (Exception exception)
                {
                    Logger.LogError(
                        exception,
                        "Readiness could not be refreshed for assessment {AssessmentId}; other lifecycle metrics will continue loading.",
                        item.AssessmentId);
                }
            }

            if (lifecycleChanged)
            {
                LifecycleMetrics = await DashboardDataService.GetKPIMetricsAsync(userId);
            }
        }

        private async Task<long> ResolveAuthenticatedUserIdAsync()
        {
            if (sessionService?.IsAuthenticated == true && sessionService.UserId > 0)
            {
                return sessionService.UserId;
            }

            var authenticationState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
            if (authenticationState.User.Identity?.IsAuthenticated != true)
            {
                return 0;
            }

            var applicationUser = await AuthenticationService.GetCurrentUserAsync(authenticationState.User);
            return applicationUser?.Id ?? 0;
        }

        private async Task ViewReadinessAsync(AssessmentDto assessment)
        {
            await OffcanvasService!.ShowAsync(new CanvasRequest
            {
                Title = $"Projection Readiness - {assessment.CaseNumber}",
                Width = 850,
                ComponentType = typeof(AssessmentProjectionReadinessComponent),
                Parameters = new Dictionary<string, object>
                {
                    { nameof(AssessmentProjectionReadinessComponent.AssessmentId), assessment.AssessmentId }
                }
            });
        }

        private async Task MarkCompleteAsync(AssessmentDto assessmentDto)
        {
            if (assessmentDto.StatusId != AssessmentLifecycleStatus.ReadyForReview)
            {
                _Toast!.ShowError("Submit the assessment for review before marking it complete.", "Workflow check");
                return;
            }

            var readiness = await readinessService.EvaluateAsync(assessmentDto.AssessmentId);
            await PersistReadinessAsync(assessmentDto.AssessmentId, readiness.Score);

            if (!readiness.CanComplete)
            {
                _Toast!.ShowError(
                    $"This assessment has {readiness.Checks.Count(item => item.Required && !item.Passed)} blocking readiness item(s).",
                    "Assessment not ready");
                await ViewReadinessAsync(assessmentDto);
                return;
            }

            var confirmed = LifecycleConfirmDialog is not null
                && await LifecycleConfirmDialog.ShowAsync(
                    "Complete assessment?",
                    $"{assessmentDto.CaseNumber} will be marked complete and treated as finalized in assessment metrics.",
                    "Mark Complete",
                    "Keep Open");
            if (!confirmed)
                return;

            await UpdateStatusAsync(assessmentDto.AssessmentId, AssessmentLifecycleStatus.Completed, true);
            _Toast!.ShowSuccess($"{assessmentDto.CaseNumber} has been marked complete.", sessionService!.AppTitle);
            await RefreshAssessmentWorkspaceAsync();
        }

        private async Task SubmitForReviewAsync(AssessmentDto assessmentDto)
        {
            var readiness = await readinessService.EvaluateAsync(assessmentDto.AssessmentId);
            await PersistReadinessAsync(assessmentDto.AssessmentId, readiness.Score);
            if (!readiness.CanComplete)
            {
                _Toast!.ShowError("Required projection inputs are still incomplete.", "Assessment not ready");
                await ViewReadinessAsync(assessmentDto);
                return;
            }

            await UpdateStatusAsync(assessmentDto.AssessmentId, AssessmentLifecycleStatus.ReadyForReview);
            _Toast!.ShowSuccess($"{assessmentDto.CaseNumber} is ready for review.", sessionService!.AppTitle);
            await RefreshAssessmentWorkspaceAsync();
        }

        private async Task ReopenAsync(AssessmentDto assessmentDto)
        {
            var confirmed = LifecycleConfirmDialog is not null
                && await LifecycleConfirmDialog.ShowAsync(
                    "Re-open assessment?",
                    $"{assessmentDto.CaseNumber} will return to In Progress and can be edited again.",
                    "Re-open",
                    "Cancel");
            if (!confirmed)
                return;

            await UpdateStatusAsync(assessmentDto.AssessmentId, AssessmentLifecycleStatus.InProgress, false, true);
            _Toast!.ShowSuccess($"{assessmentDto.CaseNumber} has been re-opened.", sessionService!.AppTitle);
            await RefreshAssessmentWorkspaceAsync();
        }

        private async Task PersistReadinessAsync(long assessmentId, int score)
        {
            var assessment = await coreAssessmentRepository.GetByIdAsync(assessmentId);
            if (assessment is null)
                throw new InvalidOperationException($"Assessment {assessmentId} was not found.");

            if (assessment.ProgressPercentage != score)
            {
                assessment.ProgressPercentage = score;
                await coreAssessmentRepository.SaveAsync(assessment);
            }
        }

        private async Task UpdateStatusAsync(
            long assessmentId,
            long statusId,
            bool completing = false,
            bool reopening = false)
        {
            var assessment = await coreAssessmentRepository.GetByIdAsync(assessmentId)
                ?? throw new InvalidOperationException($"Assessment {assessmentId} was not found.");
            assessment.StatusId = statusId;
            assessment.ModifiedDate = DateTime.UtcNow;
            assessment.ModifiedBy = sessionService?.UserId ?? assessment.ModifiedBy;
            if (completing)
            {
                assessment.CompletedDate = assessment.ModifiedDate;
                assessment.CompletedBy = sessionService?.UserId;
            }
            if (reopening)
            {
                assessment.ReopenedDate = assessment.ModifiedDate;
                assessment.ReopenedBy = sessionService?.UserId;
                assessment.CompletedDate = null;
                assessment.CompletedBy = null;
            }
            await coreAssessmentRepository.SaveAsync(assessment);
        }

        private async Task RefreshAssessmentWorkspaceAsync()
        {
            await RefreshLifecycleMetricsAsync();
            if (assessmentTable is not null)
                await assessmentTable.RefreshAsync();
            StateHasChanged();
        }

        private Task HandleLifecycleDrill(string kpiType)
        {
            _Toast!.ShowInfo($"{kpiType.Replace("Assessments", " assessments")} is shown in the registry below.", sessionService!.AppTitle);
            return Task.CompletedTask;
        }

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