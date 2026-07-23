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

       private long AssessmentId { get; set; } = new();
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
                ExistingRepaymentIds.Clear();

                if (AssessmentLoanId > 0 && ViqCrudService != null)
                {
                    // Fetch existing repayment entries for this Loan from database view/table
                    var existingRecords = await ViqCrudService.GetListAsync<AssessmentLoanRepaymentDto>(
                        "vw_AssessmentLoanRepayment_list",
                        new { AssessmentLoanId = AssessmentLoanId },
                        "MetricTypeId"
                    );

                    if (existingRecords != null && existingRecords.Any())
                    {
                        var firstRow = existingRecords.First();
                        loanTypeName = firstRow.LoanTypeName ?? loanTypeName;

                        var expectedRow = existingRecords.FirstOrDefault(r => r.MetricTypeId == 1);
                        var interestRow = existingRecords.FirstOrDefault(r => r.MetricTypeId == 2);
                        var extraRow = existingRecords.FirstOrDefault(r => r.MetricTypeId == 3);

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

                        // Determine start month dynamically from expected repayment values
                        for (int m = 0; m < 12; m++)
                        {
                            if (monthlyCells[m].Expected > 0)
                            {
                                startMonth = m + 1;
                                break;
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
                // Fall-safe empty model initialization using correct ViewModel type
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

                long assessmentId = sessionService?.AssessmentId ?? 0;
                int[] metricTypes = { 1, 2, 3 };

                foreach (var metricTypeId in metricTypes)
                {
                    long repaymentId = ExistingRepaymentIds.ContainsKey(metricTypeId) ? ExistingRepaymentIds[metricTypeId] : 0;

                    var repaymentEntity = new AssessmentLoanRepayment
                    {
                        AssessmentLoanRepaymentId = repaymentId,
                        AssessmentId = assessmentId,
                        AssessmentLoanId = ActiveRepaymentModel.AssessmentLoanId,
                        MetricTypeId = metricTypeId,
                        CreatedDate = DateTime.UtcNow,
                        ModifiedDate = DateTime.UtcNow
                    };

                    decimal[] values = new decimal[12];
                    for (int i = 0; i < 12; i++)
                    {
                        values[i] = metricTypeId switch
                        {
                            1 => ActiveRepaymentModel.MonthlyLines[i].Expected,
                            2 => ActiveRepaymentModel.MonthlyLines[i].Interest,
                            3 => ActiveRepaymentModel.MonthlyLines[i].Extra,
                            _ => 0m
                        };
                    }
                    repaymentEntity.MonthlyValues = values;

                    await DataRepository.SaveAsync(repaymentEntity);
                }

                await zabCanvasService!.PublishResultAsync(SaveResult.SavedAndClose("Loan details saved successfully."));
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

    //public class RepaymentMetricCell
    //{
    //    public decimal Expected { get; set; }
    //    public decimal Interest { get; set; }
    //    public decimal Extra { get; set; }
    //    public decimal Total => Expected + Interest + Extra;
    //}
}