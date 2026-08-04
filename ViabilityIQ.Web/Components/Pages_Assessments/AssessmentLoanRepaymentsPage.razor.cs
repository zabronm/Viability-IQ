using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ViabilityIQ.Application.Interfaces;
using ViabilityIQ.Infrastructure.Repositories;
using ViabilityIQ.Shared.DataModels;
using ViabilityIQ.Shared.SharedModels;
using ViabilityIQ.Web.Components.CommonComponents;
using ViabilityIQ.Web.Components.Pages_Assessments.PageFormComponents;
using ViabilityIQ.Web.Services;
using static ViabilityIQ.Web.Components.CommonComponents.ViqAlertComponent;

namespace ViabilityIQ.Web.Components.Pages_Assessments
{
    public partial class AssessmentLoanRepaymentsPage : ComponentBase, IAsyncDisposable
    {
        #region Injected Services
        [Inject] ToastService _Toast { get; set; } = default!;
        [Inject] ZabOffCanvasService zabCanvasService { get; set; } = default!;
        [Inject] ISessionService? sessionService { get; set; }
        [Inject] MasterDataService? ViqCrudService { get; set; }
        [Inject] IProjectionStateManager? projectionStateManager { get; set; }
        [Inject] ILogger<AssessmentLoanRepaymentsPage>? Logger { get; set; }
        #endregion

        #region Parameters
        [Parameter] public long AssessmentId { get; set; }

        #endregion

        #region Private Fields

        private bool blAlert { get; set; } = false;
        private AlertSeverity AlertSeverity { get; set; } = AlertSeverity.Warning;
        private string AlertHeading { get; set; } = "Loan Notice:";
        private string AlertMessage { get; set; } = "Welcome to the loans module. Please note that calculations are done based on selected loan calculation method";

        private ZabConfirmDialogComponent? ConfirmDeleteDialog { get; set; } = default!;
        private string StatusMessage { get; set; } = "";

        private string SearchQuery { get; set; } = string.Empty;
        private long? SelectedLoanTypeId { get; set; } = null;
        private bool IsLoading { get; set; } = false;
        private string ActivePanelTitle { get; set; } = "Loan Administration Module";

        private List<LoanRepaymentRowViewModel> LoanProfilesDataset { get; set; } = new();
        private List<AssessmentLoanRepaymentDto> LoanTypeLookupList { get; set; } = new();

        #endregion

        #region Lifecycle Methods

        protected override async Task OnInitializedAsync()
        {
            try
            {
                AssessmentId = sessionService?.AssessmentId ?? 0;

                Logger?.LogInformation(
                    "AssessmentLoanRepaymentsPage initialized for assessment {AssessmentId}",
                    AssessmentId);

                await LoadLookupDataAsync();
                await LoadLoanRepaymentsDataAsync();

                // Subscribe to projection changes
                if (projectionStateManager != null)
                {
                    projectionStateManager.ProjectionChanged += OnProjectionChanged;

                    Logger?.LogDebug("AssessmentLoanRepaymentsPage subscribed to ProjectionChanged events");
                }
            }
            catch (Exception ex)
            {
                Logger?.LogError(ex, "Error initializing AssessmentLoanRepaymentsPage");
                _Toast?.ShowError(ex.Message, sessionService?.AppTitle);
            }
        }

        #endregion

        #region Private Methods

        private async Task LoadLookupDataAsync()
        {
            try
            {
                Logger?.LogDebug("Loading loan type lookup data");

                var list = await ViqCrudService!.GetListAsync<AssessmentLoanRepaymentDto>("vw_loantypes_lookup", new { }, "Name");
                LoanTypeLookupList = list?.ToList() ?? new();
                blAlert = true;

                Logger?.LogDebug("Loaded {LoanCount} loan types", LoanTypeLookupList.Count);
            }
            catch (Exception ex)
            {
                Logger?.LogError(ex, "Error loading loan type lookup data");
                LoanTypeLookupList = new();
            }
        }

        private async Task LoadLoanRepaymentsDataAsync()
        {
            try
            {
                IsLoading = true;
                StateHasChanged();

                Logger?.LogDebug("Loading loan repayments for assessment {AssessmentId}", AssessmentId);

                var rawData = await ViqCrudService!.GetListAsync<AssessmentLoanRepaymentDto>(
                    "vw_assessment_loan_repayment_list",
                    new { AssessmentId = sessionService?.AssessmentId ?? 0, Active = true },
                    "AssessmentLoanId"
                );

                if (rawData != null && rawData.Any())
                {
                    LoanProfilesDataset = rawData
                        .GroupBy(x => x.AssessmentLoanId)
                        .Select(group =>
                        {
                            var expectedRow = group.FirstOrDefault(r => r.MetricTypeId == 1);
                            var interestRow = group.FirstOrDefault(r => r.MetricTypeId == 2);
                            var extraRow = group.FirstOrDefault(r => r.MetricTypeId == 3);

                            var firstRow = group.First();

                            var monthlyCells = new List<RepaymentMetricCell>();
                            for (int i = 1; i <= 12; i++)
                            {
                                decimal expVal = GetMonthVal(expectedRow, i);
                                decimal intVal = GetMonthVal(interestRow, i);
                                decimal extVal = GetMonthVal(extraRow, i);

                                monthlyCells.Add(new RepaymentMetricCell
                                {
                                    Expected = expVal,
                                    Interest = intVal,
                                    Extra = extVal
                                });
                            }

                            return new LoanRepaymentRowViewModel
                            {
                                LoanId = group.Key,
                                LoanTypeName = firstRow.LoanTypeName ?? "Unnamed Loan Profile",
                                BankName = firstRow.BankName ?? "Unknown Institution",
                                StartMonth = firstRow.StartMonth,
                                MonthlyData = monthlyCells
                            };
                        }).ToList();

                    Logger?.LogInformation(
                        "Loaded {LoanCount} loan profiles for assessment {AssessmentId}",
                        LoanProfilesDataset.Count, AssessmentId);
                }
                else
                {
                    LoanProfilesDataset = new();
                    Logger?.LogWarning("No loan profiles found for assessment {AssessmentId}", AssessmentId);
                }
            }
            catch (Exception ex)
            {
                Logger?.LogError(ex, "Error loading loan repayments for assessment {AssessmentId}", AssessmentId);
                _Toast?.ShowError("Could not retrieve loan repayment schedules.", sessionService?.AppTitle ?? "Viability.IQ");
                LoanProfilesDataset = new();
            }
            finally
            {
                IsLoading = false;
                StateHasChanged();
            }
        }

        private decimal GetMonthVal(AssessmentLoanRepaymentDto? row, int monthIndex)
        {
            if (row == null) return 0m;
            return monthIndex switch
            {
                1 => row.Month_1,
                2 => row.Month_2,
                3 => row.Month_3,
                4 => row.Month_4,
                5 => row.Month_5,
                6 => row.Month_6,
                7 => row.Month_7,
                8 => row.Month_8,
                9 => row.Month_9,
                10 => row.Month_10,
                11 => row.Month_11,
                12 => row.Month_12,
                _ => 0m
            };
        }

        private string FormatLoanName(string name)
        {
            if (string.IsNullOrEmpty(name)) return string.Empty;
            if (name.Length <= 20) return name;
            return name.Substring(0, 18) + "..";
        }

        private void OnLoanTypeFilterChanged(long? selectedValue)
        {
            SelectedLoanTypeId = selectedValue;
            StateHasChanged();
        }

        private IEnumerable<LoanRepaymentRowViewModel> GetFilteredLoanProfiles()
        {
            var query = LoanProfilesDataset.AsEnumerable();

            if (SelectedLoanTypeId.HasValue && SelectedLoanTypeId.Value > 0)
            {
                query = query.Where(x => x.LoanId == SelectedLoanTypeId.Value);
            }

            if (!string.IsNullOrWhiteSpace(SearchQuery))
            {
                query = query.Where(x =>
                    x.LoanTypeName.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase) ||
                    x.BankName.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase));
            }

            return query;
        }

        private void OnProjectionChanged(object sender, ProjectionChangedEventArgs e)
        {
            if (e.AssessmentId == AssessmentId)
            {
                Logger?.LogInformation(
                    "Projection changed event received for assessment {AssessmentId}, reloading loans",
                    AssessmentId);

                InvokeAsync(async () => await LoadLoanRepaymentsDataAsync());
            }
        }

        private async Task OpenLoanFormAsync(long assessmentLoanId)
        {
            try
            {
                ActivePanelTitle = assessmentLoanId == 0 ? "Add Assessment Loan" : "Edit Assessment Loan";

                Logger?.LogDebug("Opening loan form for AssessmentLoanId {AssessmentLoanId}", assessmentLoanId);

                await zabCanvasService.ShowAsync(
                    new CanvasRequest
                    {
                        Title = ActivePanelTitle,
                        Width = 360,
                        ComponentType = typeof(AssessmentLoanFormComponent),
                        Parameters = new
                        {
                            AssessmentLoanId = assessmentLoanId,
                            AssessmentId = AssessmentId
                        },
                        ResultCallback = HandleNewLoanSaveAsync
                    });
            }
            catch (Exception ex)
            {
                Logger?.LogError(ex, "Error opening loan form");
            }
        }

        private async Task OpenRepaymentsFormAsync(long loanRepaymentId)
        {
            try
            {
                ActivePanelTitle = "Edit Loan Repayment Schedule";

                var targetLoan = LoanProfilesDataset.FirstOrDefault(x => x.LoanId == loanRepaymentId);
                string displayLoanName = targetLoan != null ? $"{targetLoan.LoanTypeName} ({targetLoan.BankName})" : "Loan Repayment Schedule";

                Logger?.LogDebug("Opening repayment form for LoanId {LoanId}", loanRepaymentId);

                await zabCanvasService.ShowAsync(
                    new CanvasRequest
                    {
                        Title = ActivePanelTitle,
                        Width = 550,
                        ComponentType = typeof(AssessmentLoanRepaymentFormComponent),
                        Parameters = new
                        {
                            AssessmentLoanId = loanRepaymentId,
                            parLoanTypeName = displayLoanName,
                        },
                        ResultCallback = HandleLoanRepaymentResultAsync
                    });
            }
            catch (Exception ex)
            {
                Logger?.LogError(ex, "Error opening repayment form");
            }
        }

        private async Task HandleDeleteLoanAndRepaymentsAsync(long loanRepaymentId)
        {
            try
            {
                StatusMessage = "Awaiting user confirmation...";

                Logger?.LogDebug("Requesting confirmation to delete loan {LoanId}", loanRepaymentId);

                bool isConfirmed = await ConfirmDeleteDialog!.ShowAsync(
                    title: "Delete Loan Permanently?",
                    message: "Confirm you want to delete this loan and its schedules?",
                    confirmText: "Yes Delete",
                    cancelText: " No, Keep it"
                );

                if (!isConfirmed)
                {
                    StatusMessage = "Deletion cancelled by user.";
                    Logger?.LogInformation("Deletion cancelled for loan {LoanId}", loanRepaymentId);
                    return;
                }

                Logger?.LogInformation("Deleting loan {LoanId} for assessment {AssessmentId}", loanRepaymentId, AssessmentId);

                var str_sql = "UPDATE tblAssessmentLoanRepayment SET [Active]=@parActive WHERE (AssessmentLoanId = @parAssessmentLoanId); " +
                              "UPDATE tblAssessmentLoan SET [Active]=@parActive WHERE (AssessmentLoanId = @parAssessmentLoanId);";

                _ = await ViqCrudService!.ExecuteCommandAsync(str_sql, new { parActive = false, parAssessmentLoanId = loanRepaymentId });

                await LoadLoanRepaymentsDataAsync();

                // ✅ TRIGGER CASHFLOW RECALCULATION
                Logger?.LogInformation("Invalidating cashflow after loan deletion for assessment {AssessmentId}", AssessmentId);
                await projectionStateManager!.InvalidateDataAsync("loans", AssessmentId, AssessmentId);

                _Toast?.ShowSuccess("Loan/repayments deleted successfully.", sessionService!.AppTitle);
            }
            catch (Exception ex)
            {
                Logger?.LogError(ex, "Error deleting loan {LoanId}", loanRepaymentId);
                _Toast?.ShowError($"Error deleting loan and repayments: {ex.Message}", sessionService!.AppTitle);
            }
        }

        private async Task HandleNewLoanSaveAsync(SaveResult result)
        {
            if (result.Success)
            {
                Logger?.LogInformation("Loan saved successfully for assessment {AssessmentId}", AssessmentId);
                await LoadLoanRepaymentsDataAsync();

                // ✅ TRIGGER CASHFLOW RECALCULATION
                Logger?.LogInformation("Invalidating cashflow after loan save for assessment {AssessmentId}", AssessmentId);
                await projectionStateManager!.InvalidateDataAsync("loans", AssessmentId, AssessmentId);

                _Toast?.ShowSuccess(result.Message, sessionService!.AppTitle);
            }
            else if (!result.Success && !result.Cancelled)
            {
                _Toast?.ShowError(result.Message, sessionService!.AppTitle);
            }
        }

        private async Task HandleLoanRepaymentResultAsync(SaveResult result)
        {
            if (result.Success)
            {
                Logger?.LogInformation("Loan repayment schedule saved for assessment {AssessmentId}", AssessmentId);
                await LoadLoanRepaymentsDataAsync();

                // ✅ TRIGGER CASHFLOW RECALCULATION
                Logger?.LogInformation("Invalidating cashflow after repayment save for assessment {AssessmentId}", AssessmentId);
                await projectionStateManager!.InvalidateDataAsync("loans", AssessmentId, AssessmentId);

                _Toast?.ShowSuccess(result.Message, sessionService!.AppTitle);
            }
            else if (!result.Success && !result.Cancelled)
            {
                _Toast?.ShowError(result.Message, sessionService!.AppTitle);
            }
        }

        private decimal GetMonthlyGrandTotal(int monthIndex)
        {
            if (LoanProfilesDataset == null || !LoanProfilesDataset.Any()) return 0m;

            return LoanProfilesDataset.Sum(loan =>
                loan.MonthlyData.Count > monthIndex ? loan.MonthlyData[monthIndex].Total : 0m);
        }

        private decimal GetAccumulatedGrandTotal()
        {
            if (LoanProfilesDataset == null || !LoanProfilesDataset.Any()) return 0m;

            return LoanProfilesDataset.Sum(loan => loan.MonthlyData.Sum(x => x.Total));
        }

        #endregion

        #region Disposal

        async ValueTask IAsyncDisposable.DisposeAsync()
        {
            try
            {
                if (projectionStateManager != null)
                {
                    projectionStateManager.ProjectionChanged -= OnProjectionChanged;
                    Logger?.LogDebug("AssessmentLoanRepaymentsPage unsubscribed from ProjectionChanged events");
                }
            }
            catch (Exception ex)
            {
                Logger?.LogError(ex, "Error disposing AssessmentLoanRepaymentsPage");
            }

            await Task.CompletedTask;
        }

        #endregion
    }

    #region View Models

    public class LoanRepaymentRowViewModel
    {
        public long LoanId { get; set; }
        public string LoanTypeName { get; set; } = string.Empty;
        public int StartMonth { get; set; }
        public string BankName { get; set; } = string.Empty;
        public List<RepaymentMetricCell> MonthlyData { get; set; } = new();
    }

    public class RepaymentMetricCell
    {
        public decimal Expected { get; set; }
        public decimal Interest { get; set; }
        public decimal Extra { get; set; }
        public decimal Total => Expected + Interest + Extra;
    }

    #endregion
}