using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Identity;
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
    public partial class AssessmentLoanRepaymentsPage
    {
        [Inject] ToastService _Toast { get; set; } = default!;
        [Inject] ZabOffCanvasService zabCanvasService { get; set; } = default!;
        [Inject] ISessionService? sessionService { get; set; }
        [Inject] MasterDataService? ViqCrudService { get; set; }

        private long currentAssessmentId { get; set; }
        private bool blAlert { get; set; } = false;
        private AlertSeverity AlertSeverity { get; set; } = AlertSeverity.Warning;
        private string AlertHeading { get; set; } = "Loan Notice:";
        private string AlertMessage { get; set; } = "";

        private string SearchQuery { get; set; } = string.Empty;
        private long? SelectedLoanTypeId { get; set; } = null;
        private bool IsLoading { get; set; } = false;
        private string ActivePanelTitle { get; set; } = "Loan Administration Module";

        private List<LoanRepaymentRowViewModel> LoanProfilesDataset { get; set; } = new();
        private List<AssessmentLoanRepaymentDto> LoanTypeLookupList { get; set; } = new();

        protected override async Task OnInitializedAsync()
        {
            currentAssessmentId = currentAssessmentId == 0L ? currentAssessmentId : sessionService!.AssessmentId!.Value; 
            await LoadLookupDataAsync();
            await LoadLoanRepaymentsDataAsync();
        }

        private async Task LoadLookupDataAsync()
        {
            try
            {
                // Load lookup data for the Loan Type filter dropdown component
                var list = await ViqCrudService!.GetListAsync<AssessmentLoanRepaymentDto>("vw_loantypes_lookup", new { }, "Name");
                LoanTypeLookupList = list?.ToList() ?? new();

                blAlert = true;
            }
            catch (Exception)
            {
                LoanTypeLookupList = new();
            }
        }

        private async Task LoadLoanRepaymentsDataAsync()
        {
            try
            {
                IsLoading = true;
                StateHasChanged();

                // 1) Read rows from the view mapped to AssessmentLoanRepaymentDto
                var rawData = await ViqCrudService!.GetListAsync<AssessmentLoanRepaymentDto>(
                    "vw_assessment_loan_repayment_list",
                    new { AssessmentId = sessionService?.AssessmentId ?? 0, Active = true },
                    "AssessmentLoanId"
                );

                if (rawData != null && rawData.Any())
                {
                    // Group rows by individual Loan (AssessmentLoanId)
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

                            // Determine start month dynamically (first month where expected repayment > 0, defaults to 1 if none found)
                            //int calculatedStartMonth = 1;
                            //for (int m = 0; m < 12; m++)
                            //{
                            //    if (monthlyCells[m].Expected > 0)
                            //    {
                            //        calculatedStartMonth = m + 1;
                            //        break;
                            //    }
                            //}
                            

                            return new LoanRepaymentRowViewModel
                            {
                                LoanId = group.Key,
                                LoanTypeName = firstRow.LoanTypeName ?? "Unnamed Loan Profile",
                                BankName = firstRow.BankName ?? "Unknown Institution",
                                /*StartMonth = calculatedStartMonth*/
                                 StartMonth = firstRow.StartMonth,       // no longer calculated, but specified by the user, 1 is assumed if null
                                MonthlyData = monthlyCells
                            };
                        }).ToList();
                }
                else
                {
                    LoanProfilesDataset = new();
                }
            }
            catch (Exception ex)
            {
                _Toast.ShowError("Could not retrieve loan repayment schedules.", sessionService?.AppTitle ?? "Viability.IQ");
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

            // Filter by Dropdown LoanType if selected (assuming LoanId maps or matches filter context)
            if (SelectedLoanTypeId.HasValue && SelectedLoanTypeId.Value > 0)
            {
                query = query.Where(x => x.LoanId == SelectedLoanTypeId.Value);
            }

            // Filter by Search Query string
            if (!string.IsNullOrWhiteSpace(SearchQuery))
            {
                query = query.Where(x =>
                    x.LoanTypeName.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase) ||
                    x.BankName.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase));
            }

            return query;
        }

        private async Task OpenLoanFormAsync(long assessmentLoanId)
        {
            try
            {
                ActivePanelTitle = assessmentLoanId == 0 ? "Add Assessment Loan" : "Edit Assessment Loan";

                await zabCanvasService.ShowAsync(
                    new CanvasRequest
                    {
                        Title = ActivePanelTitle,
                        Width = 360,
                        ComponentType = typeof(AssessmentLoanFormComponent),
                        Parameters = new { 
                            AssessmentLoanId = assessmentLoanId,
                            AssessmentId = currentAssessmentId         
                        },
                        ResultCallback = HandleNewLoanSaveAsync
                    });
            }
            catch (Exception) { }
        }

        private async Task OpenRepaymentsFormAsync(long loanRepaymentId)
        {
            try
            {
                ActivePanelTitle = "Edit Loan Repayment Schedule";

                await zabCanvasService.ShowAsync(
                    new CanvasRequest
                    {
                        Title = ActivePanelTitle,
                        Width = 550,
                        ComponentType = typeof(AssessmentLoanRepaymentFormComponent),
                        Parameters = new { AssessmentLoanId = loanRepaymentId },
                        ResultCallback = HandleLoanRepaymentResultAsync
                    });
            }
            catch (Exception) { }
        }

        private async Task HandleDeleteLoanAndRepaymentsAsync(long loanRepaymentId)
        {
            try
            {    

                var str_sql = "UPDATE tblAssessmentLoanRepayment SET [Active]=@parActive WHERE (AssessmentLoanId = @parAssessmentLoanId); " +
                              "UPDATE tblAssessmentLoan SET [Active]=@parActive WHERE (AssessmentLoanId = @parAssessmentLoanId);";

                _= await ViqCrudService!.ExecuteCommandAsync(str_sql, new { parActive = false, parAssessmentLoanId = loanRepaymentId });

                _ = LoadLoanRepaymentsDataAsync();

                _Toast.ShowSuccess("Loan and repayments deleted successfully.", sessionService!.AppTitle);

            }
            catch (Exception ex)
            {
                _Toast.ShowError($"Error deleting loan and repayments: {ex.Message}", sessionService!.AppTitle);
            }
        }
        


        private async Task HandleNewLoanSaveAsync(SaveResult result)
        {
            if (result.Success)
            {
                await LoadLoanRepaymentsDataAsync();
                _Toast.ShowSuccess(result.Message, sessionService!.AppTitle);
            }
            else if (!result.Success && !result.Cancelled)
            {
                _Toast.ShowError(result.Message, sessionService!.AppTitle);
            }
        }

        private async Task HandleLoanRepaymentResultAsync(SaveResult result)
        {
            if (result.Success)
            {
                await LoadLoanRepaymentsDataAsync();
                _Toast.ShowSuccess(result.Message, sessionService!.AppTitle);
            }
            else if (!result.Success && !result.Cancelled)
            {
                _Toast.ShowError(result.Message, sessionService!.AppTitle);
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
    }

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
}