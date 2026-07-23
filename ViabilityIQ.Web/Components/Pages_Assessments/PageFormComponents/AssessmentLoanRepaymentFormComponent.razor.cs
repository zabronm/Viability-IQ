using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ViabilityIQ.Application.Interfaces;
using ViabilityIQ.Infrastructure.Repositories;
using ViabilityIQ.Shared.DataModels;
using ViabilityIQ.Shared.SharedModels;
using ViabilityIQ.Web.Services;

namespace ViabilityIQ.Web.Components.Pages_Assessments
{
    public partial class AssessmentLoanRepaymentFormComponent : ComponentBase
    {
        [Inject] IGenericDataRepository<AssessmentLoanRepayment>? DataRepository { get; set; }
        [Inject] public ZabOffCanvasService zabCanvasService { get; set; } = default!;
        [Inject] MasterDataService? ViqCrudService { get; set; }
        [Inject] ISessionService? sessionService { get; set; }

        [Parameter] public long AssessmentLoanId { get; set; }
        [Parameter] public string parLoanTypeName { get; set; } = string.Empty; // Added parameter
        [Parameter] public string BankName { get; set; } = string.Empty; // Added parameter


        private long AssessmentId { get; set; } = new();
        private decimal BulkExtraAmount { get; set; } = 0m;

        private RepaymentFormViewModel ActiveRepaymentModel { get; set; } = new()
        {
            MonthlyLines = Enumerable.Range(0, 12).Select(_ => new RepaymentMetricCell()).ToList()
        };

        // Holds original row IDs for updating existing records instead of duplicating them
        private Dictionary<int, long> ExistingRepaymentIds { get; set; } = new();

        protected override async Task OnParametersSetAsync()
        {
            AssessmentId = sessionService!.AssessmentId!.Value;
            await LoadIsolatedFormModelAsync();
        }

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
                    // Fetch existing repayment entries matching the exact query pattern used in the parent page list
                    var existingRecords = await ViqCrudService.GetListAsync<AssessmentLoanRepaymentDto>(
                        "vw_assessment_loan_repayment_list",
                        new { AssessmentId = AssessmentId, Active = true },
                        "AssessmentLoanId"
                    );

                    if (existingRecords != null && existingRecords.Any())
                    {
                        // Filter specifically for the current AssessmentLoanId being edited
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
            }
            catch (Exception)
            {
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

            foreach (var line in ActiveRepaymentModel.MonthlyLines)
            {
                line.Extra = BulkExtraAmount;
            }
        }

        private void ClearBulkExtraAllocation()
        {
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
            try
            {
                if (ViqCrudService == null || DataRepository == null) return;

                long assessmentId = AssessmentId > 0 ? AssessmentId : (sessionService?.AssessmentId ?? 0);

                // Persist only MetricTypeId 3 (Extra/Balloon Repayments) modified by the user
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
                }

                await zabCanvasService!.PublishResultAsync(SaveResult.SavedAndClose("Extra/Balloon repayments saved successfully."));
            }
            catch (Exception ex)
            {
                await zabCanvasService!.PublishResultAsync(new SaveResult { Success = false, Message = $"Error: {ex.Message}" });
            }
        }

        private async Task CancelFormWorkflowAsync() => await zabCanvasService!.HideAsync(SaveResult.Cancel());
    }

    public class RepaymentFormViewModel
    {
        public long AssessmentLoanId { get; set; }
        public string LoanTypeName { get; set; } = string.Empty;
        public int StartMonth { get; set; } = 1;
        public bool SendToCashbook { get; set; }
        public List<RepaymentMetricCell> MonthlyLines { get; set; } = new();
    }
}