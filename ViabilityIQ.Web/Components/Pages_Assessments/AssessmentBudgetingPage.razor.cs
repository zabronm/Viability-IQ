using Microsoft.AspNetCore.Components;
using System.Globalization;
using ViabilityIQ.Shared.DataModels.SecurityDataModels;
using ViabilityIQ.Shared.Reporting;
using ViabilityIQ.Shared.SharedModels;

namespace ViabilityIQ.Web.Components.Pages_Assessments
{
    public partial class AssessmentBudgetingPage
    {
        [Parameter] public long AssessmentId { get; set; }
        private AssessmentBudgetWorkspace? workspace;
        private IReadOnlyList<AssessmentBudgetAlert> alerts = [];
        private BudgetDisplayMode displayMode = BudgetDisplayMode.Budget;
        private bool isLoading;
        private bool isBusy;
        private bool isDirty;
        private bool canCreate;
        private long loadedAssessmentId;
        private string? pageError;
        private string newBudgetName = string.Empty;
        private string? newBudgetRemarks;

        protected override async Task OnParametersSetAsync()
        {
            if (loadedAssessmentId == AssessmentId)
            {
                return;
            }

            loadedAssessmentId = AssessmentId;
            newBudgetName = $"Annual Budget - {SessionService.CaseNumber}";
            await LoadAsync();
        }

        private async Task LoadAsync()
        {
            isLoading = true;
            pageError = null;
            try
            {
                workspace = await BudgetService.GetAsync(AssessmentId);
                canCreate = await CanWriteAsync();
                RefreshDerivedState();
            }
            catch (UnauthorizedAccessException exception)
            {
                workspace = null;
                pageError = exception.Message;
                Toast.ShowError(exception.Message);
            }
            catch (Exception exception)
            {
                workspace = null;
                pageError = "The assessment budget could not be loaded.";
                Logger.LogError(exception, "Budget load failed for assessment {AssessmentId}.", AssessmentId);
                Toast.ShowError(pageError);
            }
            finally
            {
                isLoading = false;
            }
        }

        private async Task<bool> CanWriteAsync()
        {
            try
            {
                await AuthorizationService.EnsureCanAccessAssessmentAsync(
                    AssessmentId,
                    TenantRecordAccess.Write);
                return true;
            }
            catch (UnauthorizedAccessException)
            {
                return false;
            }
        }

        private async Task CreateAsync()
        {
            await RunAsync(
                async () => workspace = await BudgetService.CreateFromProjectionAsync(new()
                {
                    AssessmentId = AssessmentId,
                    BudgetName = newBudgetName,
                    Remarks = newBudgetRemarks
                }),
                "Budget created from the assessment projection.");
        }

        private async Task SaveAsync()
        {
            if (workspace is null)
            {
                return;
            }

            await RunAsync(
                async () => workspace = await BudgetService.SaveAsync(new()
                {
                    AssessmentId = AssessmentId,
                    BudgetId = workspace.BudgetId,
                    BudgetName = workspace.BudgetName,
                    Remarks = workspace.Remarks,
                    RowVersion = workspace.RowVersion,
                    Lines = workspace.Lines
                        .Where(line => !line.IsCalculated)
                        .Select(line => new SaveAssessmentBudgetLineRequest
                        {
                            BudgetLineId = line.BudgetLineId,
                            BudgetMonths = line.BudgetMonths.ToArray()
                        })
                        .ToList()
                }),
                "Budget draft saved.");
        }

        private async Task RefreshProjectionAsync()
        {
            if (workspace is null)
            {
                return;
            }

            await RunAsync(
                async () => workspace = await BudgetService.RefreshProjectionAsync(
                    AssessmentId,
                    workspace.BudgetId,
                    workspace.RowVersion),
                "Projection comparison refreshed. Budget values were preserved.");
        }

        private async Task SubmitAsync()
        {
            if (workspace is null)
            {
                return;
            }

            if (isDirty)
            {
                Toast.ShowWarning("Save the draft before submitting it.");
                return;
            }

            await RunAsync(
                async () => workspace = await BudgetService.SubmitAsync(
                    AssessmentId,
                    workspace.BudgetId,
                    workspace.RowVersion),
                "Budget submitted for approval.");
        }

        private async Task ApproveAsync()
        {
            if (workspace is null)
            {
                return;
            }

            await RunAsync(
                async () => workspace = await BudgetService.ApproveAsync(
                    AssessmentId,
                    workspace.BudgetId,
                    workspace.RowVersion),
                "Budget approved and locked.");
        }

        private async Task ReopenAsync()
        {
            if (workspace is null)
            {
                return;
            }

            await RunAsync(
                async () => workspace = await BudgetService.ReopenAsync(
                    AssessmentId,
                    workspace.BudgetId,
                    workspace.RowVersion),
                "Budget reopened as a draft.");
        }

        private async Task RunAsync(Func<Task> action, string successMessage)
        {
            if (isBusy)
            {
                return;
            }

            isBusy = true;
            try
            {
                await action();
                isDirty = false;
                RefreshDerivedState();
                Toast.ShowSuccess(successMessage);
            }
            catch (UnauthorizedAccessException exception)
            {
                Toast.ShowError(exception.Message);
            }
            catch (InvalidOperationException exception)
            {
                Toast.ShowWarning(exception.Message);
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Budget operation failed for assessment {AssessmentId}.", AssessmentId);
                Toast.ShowError("The budget operation could not be completed. Please retry.");
            }
            finally
            {
                isBusy = false;
            }
        }

        private void UpdateBudgetName(ChangeEventArgs args)
        {
            if (workspace is null)
            {
                return;
            }

            workspace.BudgetName = args.Value?.ToString()?.Trim() ?? string.Empty;
            isDirty = true;
        }

        private void UpdateRemarks(ChangeEventArgs args)
        {
            if (workspace is null)
            {
                return;
            }

            workspace.Remarks = args.Value?.ToString()?.Trim();
            isDirty = true;
        }

        private void UpdateMonth(
            AssessmentBudgetLine line,
            int month,
            string? rawValue)
        {
            if (workspace is null || line.IsCalculated)
            {
                return;
            }

            if (!decimal.TryParse(
                    rawValue,
                    NumberStyles.Number,
                    CultureInfo.InvariantCulture,
                    out var value)
                && !decimal.TryParse(rawValue, out value))
            {
                Toast.ShowWarning("Enter a valid monthly amount.");
                return;
            }

            line.BudgetMonths[month] = Math.Max(0m, value);
            RecalculateLocal();
            isDirty = true;
        }

        private void RecalculateLocal()
        {
            if (workspace is null)
            {
                return;
            }

            var lines = workspace.Lines.ToDictionary(
                line => line.LineCode,
                StringComparer.OrdinalIgnoreCase);
            for (var month = 0; month < 12; month++)
            {
                Set(lines, "GROSS_PROFIT", month,
                    Value(lines, "REVENUE", month) - Value(lines, "COGS", month));
                Set(lines, "EBITDA", month,
                    Value(lines, "GROSS_PROFIT", month)
                    + Value(lines, "OTHER_INCOME", month)
                    - Value(lines, "OPERATING_EXPENSES", month));
                Set(lines, "EBIT", month,
                    Value(lines, "EBITDA", month) - Value(lines, "DEPRECIATION", month));
                Set(lines, "PBT", month,
                    Value(lines, "EBIT", month) - Value(lines, "INTEREST", month));
                Set(lines, "TOTAL_INFLOWS", month,
                    Value(lines, "SALES_RECEIPTS", month)
                    + Value(lines, "OTHER_RECEIPTS", month));
                Set(lines, "TOTAL_OUTFLOWS", month,
                    Value(lines, "SUPPLIER_PAYMENTS", month)
                    + Value(lines, "CASH_OPERATING_EXPENSES", month)
                    + Value(lines, "VAT_PAYMENT", month)
                    + Value(lines, "LOAN_PAYMENTS", month)
                    + Value(lines, "ASSET_PURCHASES", month));
                Set(lines, "NET_CASH", month,
                    Value(lines, "TOTAL_INFLOWS", month)
                    - Value(lines, "TOTAL_OUTFLOWS", month));
                var openingBank = month == 0
                    ? workspace.OpeningBank
                    : Value(lines, "CLOSING_BANK", month - 1);
                Set(lines, "CLOSING_BANK", month,
                    openingBank + Value(lines, "NET_CASH", month));
            }

            RefreshDerivedState();
        }

        private void RefreshDerivedState()
        {
            alerts = workspace is null
                ? []
                : BudgetService.BuildAlerts(workspace);
        }

        private decimal CellValue(AssessmentBudgetLine line, int month) =>
            displayMode switch
            {
                BudgetDisplayMode.Projection => line.ProjectedMonths[month],
                BudgetDisplayMode.Variance =>
                    line.BudgetMonths[month] - line.ProjectedMonths[month],
                _ => line.BudgetMonths[month]
            };

        private decimal DisplayTotal(AssessmentBudgetLine line)
        {
            if (line.LineCode == "CLOSING_BANK")
            {
                return displayMode switch
                {
                    BudgetDisplayMode.Projection => line.ProjectedMonths[^1],
                    BudgetDisplayMode.Variance =>
                        line.BudgetMonths[^1] - line.ProjectedMonths[^1],
                    _ => line.BudgetMonths[^1]
                };
            }

            return displayMode switch
            {
                BudgetDisplayMode.Projection => line.ProjectionTotal,
                BudgetDisplayMode.Variance => line.VarianceTotal,
                _ => line.BudgetTotal
            };
        }

        private string MonthLabel(int month) =>
            ProjectPeriodMapper.GetMonth(workspace?.AssessmentStartDate, month).CalendarLabel;

        private decimal BudgetTotal(string lineCode) =>
            workspace?.FindLine(lineCode)?.BudgetTotal ?? 0m;

        private string VarianceLabel(string lineCode)
        {
            var line = workspace?.FindLine(lineCode);
            return line is null
                ? "No comparison"
                : $"{SignedMoney(line.VarianceTotal)} vs projection";
        }

        private string? CellClass(AssessmentBudgetLine line, int month)
        {
            var value = CellValue(line, month);
            return displayMode == BudgetDisplayMode.Variance
                ? value < 0m ? "budget-negative" : value > 0m ? "budget-positive" : null
                : null;
        }

        private string TotalClass(AssessmentBudgetLine line)
        {
            var value = DisplayTotal(line);
            var varianceClass = displayMode == BudgetDisplayMode.Variance
                ? value < 0m ? "budget-negative" : value > 0m ? "budget-positive" : null
                : null;
            return $"budget-total-column {varianceClass}";
        }

        private string StatusBadgeClass =>
            workspace?.Status switch
            {
                AssessmentBudgetStatuses.Approved => "badge rounded-pill text-bg-success",
                AssessmentBudgetStatuses.Submitted => "badge rounded-pill text-bg-primary",
                _ => "badge rounded-pill text-bg-secondary"
            };

        private static string AlertClass(string severity) =>
            severity switch
            {
                "Critical" => "budget-alert budget-alert-critical",
                "Warning" => "budget-alert budget-alert-warning",
                _ => "budget-alert budget-alert-information"
            };

        private static string AlertIcon(string severity) =>
            severity switch
            {
                "Critical" => "bi bi-exclamation-octagon-fill",
                "Warning" => "bi bi-exclamation-triangle-fill",
                _ => "bi bi-check-circle-fill"
            };

        private static string ValueClass(decimal value) =>
            value < 0m ? "budget-negative" : "budget-positive";

        private static string Money(decimal value) => value.ToString("N2");

        private static string SignedMoney(decimal value) =>
            value == 0m ? "0.00" : $"{(value > 0m ? "+" : "-")}{Math.Abs(value):N2}";

        private static decimal Value(
            IReadOnlyDictionary<string, AssessmentBudgetLine> lines,
            string code,
            int month) =>
            lines.TryGetValue(code, out var line) ? line.BudgetMonths[month] : 0m;

        private static void Set(
            IReadOnlyDictionary<string, AssessmentBudgetLine> lines,
            string code,
            int month,
            decimal value)
        {
            if (lines.TryGetValue(code, out var line))
            {
                line.BudgetMonths[month] = value;
            }
        }

        private enum BudgetDisplayMode
        {
            Budget,
            Projection,
            Variance
        }
    }

}

