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
using ViabilityIQ.Web.Services;

namespace ViabilityIQ.Web.Components.Pages_Assessments.PageFormComponents
{
    public partial class AssessmentLoanRepaymentFormComponent : ComponentBase
    {
        #region Injected Dependencies

        [Inject] private IGenericDataRepository<AssessmentLoanRepayment>? DataRepository { get; set; }
        [Inject] public ZabOffCanvasService zabCanvasService { get; set; } = default!;
        [Inject] private MasterDataService? ViqCrudService { get; set; }
        [Inject] private ISessionService? sessionService { get; set; }
        [Inject] private IProjectionStateManager? projectionStateManager { get; set; }
        [Inject] private ILogger<AssessmentLoanRepaymentFormComponent>? Logger { get; set; }

        #endregion

        #region Parameters

        [Parameter] public long AssessmentLoanId { get; set; }
        [Parameter] public string parLoanTypeName { get; set; } = string.Empty;
        [Parameter] public string BankName { get; set; } = string.Empty;

        #endregion

        #region Private Fields

        private long AssessmentId { get; set; } = new();
        private decimal BulkExtraAmount { get; set; } = 0m;
        private bool IsSubmitting { get; set; } = false;

        private RepaymentFormViewModel ActiveRepaymentModel { get; set; } = new()
        {
            MonthlyLines = Enumerable.Range(0, 12).Select(_ => new RepaymentMetricCell()).ToList()
        };

        // Holds original row IDs for updating existing records
        private Dictionary<int, long> ExistingRepaymentIds { get; set; } = new();

        #endregion

        #region Lifecycle Methods

        protected override async Task OnParametersSetAsync()
        {
            try
            {
                AssessmentId = sessionService!.AssessmentId!.Value;

                Logger?.LogDebug(
                    "AssessmentLoanRepaymentFormComponent initialized for LoanId {AssessmentLoanId}", AssessmentLoanId);

                await LoadIsolatedFormModelAsync();
            }
            catch (Exception ex)
            {
                Logger?.LogError(ex, "Error initializing AssessmentLoanRepaymentFormComponent");
                throw;
            }
        }

        #endregion

        #region Private Methods

        private async Task LoadIsolatedFormModelAsync()
        {
            try
            {
                string loanTypeName = "Loan Repayment Schedule";
                int startMonth = 1;
                var monthlyCells = Enumerable.Range(0, 12).Select(_ => new RepaymentMetricCell()).ToList();

                if (!string.IsNullOrEmpty(parLoanTypeName))
                {
                    loanTypeName = parLoanTypeName;
                }

                ExistingRepaymentIds.Clear();

                if (AssessmentLoanId > 0 && ViqCrudService != null)
                {
                    Logger?.LogDebug("Loading existing repayment data for LoanId {AssessmentLoanId}", AssessmentLoanId);

                    // Fetch existing repayment entries
                    var existingRecords = await ViqCrudService.GetListAsync<AssessmentLoanRepaymentDto>(
                                                    "vw_assessment_loan_repayment_list", 
                                                    new { AssessmentId = AssessmentId, Active = true }, "AssessmentLoanId"
                    );

                    if (existingRecords != null && existingRecords.Any())
                    {
                        // Filter for current LoanId
                        var loanRecords = existingRecords.Where(r => r.AssessmentLoanId == AssessmentLoanId).ToList();

                        if (loanRecords.Any())
                        {
                            var firstRow = loanRecords.First();
                            loanTypeName = firstRow.LoanTypeName ?? loanTypeName;
                            startMonth = firstRow.StartMonth > 0 ? firstRow.StartMonth : 1;

                            var expectedRow = loanRecords.FirstOrDefault(r => r.MetricTypeId == 1);
                            var interestRow = loanRecords.FirstOrDefault(r => r.MetricTypeId == 2);
                            var extraRow = loanRecords.FirstOrDefault(r => r.MetricTypeId == 3);

                            if (expectedRow != null) ExistingRepaymentIds[1] = expectedRow.AssessmentLoanRepaymentId;
                            if (interestRow != null) ExistingRepaymentIds[2] = interestRow.AssessmentLoanRepaymentId;
                            if (extraRow != null) ExistingRepaymentIds[3] = extraRow.AssessmentLoanRepaymentId;

                            for (int i = 0; i < 12; i++)
                            {
                                int m = i + 1;
                                monthlyCells[i].Expected = GetValForMonth(expectedRow, m);
                                monthlyCells[i].Interest = GetValForMonth(interestRow, m);
                                monthlyCells[i].Extra = GetValForMonth(extraRow, m);
                            }

                            Logger?.LogDebug(
                                "Loaded repayment data for LoanId {AssessmentLoanId}. Expected: {ExpectedId}, Interest: {InterestId}, Extra: {ExtraId}",
                                AssessmentLoanId,
                                ExistingRepaymentIds.ContainsKey(1) ? ExistingRepaymentIds[1] : 0,
                                ExistingRepaymentIds.ContainsKey(2) ? ExistingRepaymentIds[2] : 0,
                                ExistingRepaymentIds.ContainsKey(3) ? ExistingRepaymentIds[3] : 0);
                        }
                    }
                }
                else
                {
                    loanTypeName = AssessmentLoanId > 0 ? $"Loan Profile #{AssessmentLoanId}" : "New Loan Repayment Profile";
                }

                ActiveRepaymentModel = new RepaymentFormViewModel
                {
                    AssessmentLoanId = AssessmentLoanId,
                    LoanTypeName = loanTypeName,
                    StartMonth = startMonth,
                    SendToCashbook = true,
                    MonthlyLines = monthlyCells
                };

                Logger?.LogInformation("Form model loaded for LoanId {AssessmentLoanId}. LoanType: {LoanTypeName}", AssessmentLoanId, loanTypeName);
            }
            catch (Exception ex)
            {
                Logger?.LogError(ex, "Error loading isolated form model for LoanId {AssessmentLoanId}", AssessmentLoanId);

                ActiveRepaymentModel = new RepaymentFormViewModel
                {
                    AssessmentLoanId = AssessmentLoanId,
                    LoanTypeName = "Loan Repayment Profile",
                    StartMonth = 1,
                    SendToCashbook = true,
                    MonthlyLines = Enumerable.Range(0, 12).Select(_ => new RepaymentMetricCell()).ToList()
                };
            }
        }

        private void ApplyBulkExtraAllocation()
        {
            if (ActiveRepaymentModel?.MonthlyLines == null) return;

            Logger?.LogDebug("Applying bulk extra allocation: {Amount}", BulkExtraAmount);

            foreach (var line in ActiveRepaymentModel.MonthlyLines)
            {
                line.Extra = BulkExtraAmount;
            }
        }

        private void ClearBulkExtraAllocation()
        {
            Logger?.LogDebug("Clearing bulk extra allocation");

            BulkExtraAmount = 0m;
            if (ActiveRepaymentModel?.MonthlyLines == null) return;

            foreach (var line in ActiveRepaymentModel.MonthlyLines)
            {
                line.Extra = 0m;
            }
        }

        private decimal GetValForMonth(AssessmentLoanRepaymentDto? row, int month)
        {
            if (row == null) return 0m;
            return month switch
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

        private async Task SubmitFormWorkflowAsync()
        {
            if (IsSubmitting) return;

            try
            {
                IsSubmitting = true;

                if (ViqCrudService == null || DataRepository == null)
                {
                    Logger?.LogError("Required services are null");
                    return;
                }

                long assessmentId = AssessmentId > 0 ? AssessmentId : (sessionService?.AssessmentId ?? 0);

                Logger?.LogInformation(
                    "Saving loan repayment for LoanId {AssessmentLoanId}, AssessmentId {AssessmentId}",
                    ActiveRepaymentModel.AssessmentLoanId, assessmentId);

                // Persist only MetricTypeId 3 (Extra/Balloon Repayments) modified by user
                int[] metricTypesToSave = { 3 };

                foreach (var metricTypeId in metricTypesToSave)
                {
                    long repaymentId = ExistingRepaymentIds.ContainsKey(metricTypeId) ? ExistingRepaymentIds[metricTypeId] : 0;

                    var repaymentEntity = new AssessmentLoanRepayment
                    {
                        AssessmentLoanRepaymentId = repaymentId,
                        AssessmentId = assessmentId,
                        AssessmentLoanId = ActiveRepaymentModel.AssessmentLoanId,
                        MetricTypeId = metricTypeId,
                        Active = true,
                        CreatedDate = DateTime.UtcNow,
                        ModifiedDate = DateTime.UtcNow
                    };

                    decimal[] values = new decimal[12];
                    for (int i = 0; i < 12; i++)
                    {
                        values[i] = ActiveRepaymentModel.MonthlyLines[i].Extra;
                    }
                    repaymentEntity.MonthlyValues = values;

                    await DataRepository.SaveAsync(repaymentEntity);

                    Logger?.LogDebug(
                        "Saved repayment for MetricTypeId {MetricTypeId}, RepaymentId {RepaymentId}",
                        metricTypeId, repaymentId);
                }

                // ✅ TRIGGER CASHFLOW RECALCULATION
                Logger?.LogInformation(
                    "Invalidating cashflow after repayment save for assessment {AssessmentId}",
                    assessmentId);

                await projectionStateManager!.InvalidateDataAsync("loans", assessmentId, assessmentId);

                await zabCanvasService!.PublishResultAsync(SaveResult.SavedAndClose("Extra/Balloon repayments saved successfully."));
            }
            catch (Exception ex)
            {
                Logger?.LogError(ex, "Error submitting loan repayment form");
                await zabCanvasService!.PublishResultAsync(
                    new SaveResult { Success = false, Message = $"Error: {ex.Message}" });
            }
            finally
            {
                IsSubmitting = false;
            }
        }

        private async Task CancelFormWorkflowAsync()
        {
            Logger?.LogDebug("Cancelling loan repayment form");
            await zabCanvasService!.HideAsync(SaveResult.Cancel());
        }

        #endregion
    }

    #region View Models

    public class RepaymentFormViewModel
    {
        public long AssessmentLoanId { get; set; }
        public string LoanTypeName { get; set; } = string.Empty;
        public int StartMonth { get; set; } = 1;
        public bool SendToCashbook { get; set; }
        public List<RepaymentMetricCell> MonthlyLines { get; set; } = new();
    }

    #endregion
}