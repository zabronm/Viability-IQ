using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ViabilityIQ.Application.Interfaces;
using ViabilityIQ.Shared.DataModels;
using ViabilityIQ.Shared.SharedModels;
using ViabilityIQ.Web.Components.CommonComponents;
using ViabilityIQ.Web.Components.Pages_Assessments.PageFormComponents;
using ViabilityIQ.Web.Services;

namespace ViabilityIQ.Web.Components.Pages_Assessments
{
    public partial class AssessmentSettingsPage
    {
        [Inject] private ISessionService? sessionService { get; set; }
        [Inject] private ToastService _Toast { get; set; } = default!;
        [Inject] private IGenericDataRepository<Assessment> DataRepository { get; set; } = default!;

        [Parameter] public long ActiveAssessmentId { get; set; }

        private SalesCategoryListComponent? SalesCategoryListRef;
        private AssessmentLoansListComponent? AssessmentLoansListRef;

        private Assessment? Model { get; set; }
        private bool IsLoading { get; set; } = true;
        private bool IsSubmitting { get; set; } = false;
        private string ValidationAlertMessage { get; set; } = string.Empty;

        private ZabOffCanvas? OffCanvasControlRef { get; set; }
        private string ActivePanelTitle { get; set; } = string.Empty;
        private Type? ActiveFormType { get; set; }
        private Dictionary<string, object> ActiveFormParameters { get; set; } = new();

        private decimal CalculatedCreditors0To30 => Math.Max(0m, 100m - ((Model?.Creditors_60 ?? 0m) + (Model?.Creditors_90 ?? 0m) + (Model?.Creditors_120 ?? 0m) + (Model?.Creditors_120Plus ?? 0m)));
        private decimal CalculatedDebtors0To30 => Math.Max(0m, 100m - ((Model?.Debtors_60 ?? 0m) + (Model?.Debtors_90 ?? 0m) + (Model?.Debtors_120 ?? 0m) + (Model?.Debtors_120Plus ?? 0m)));
        private decimal TotalCalculatedDirectorWages => (Model?.NumberOfDirectors ?? 0) * (Model?.MonthlyDirectorWagesAmount ?? 0m);

        protected override async Task OnParametersSetAsync()
        {
            if (sessionService?.AssessmentId != null)
            {
                ActiveAssessmentId = sessionService.AssessmentId.Value;
                await HydrateAssessmentRecordAsync();
            }
            else
            {
                _Toast.ShowError("Case number is unknown, please restart your application.");
            }
        }

        private async Task HydrateAssessmentRecordAsync()
        {
            try
            {
                IsLoading = true;
                var record = await DataRepository.GetByIdAsync(ActiveAssessmentId);
                if (record != null)
                {
                    Model = record;
                }
                else
                {
                    _Toast.ShowError("Could not retrieve the matching assessment tracking record context.");
                }
            }
            catch (Exception ex)
            {
                _Toast.ShowError($"Error hydrating configuration: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void InterceptCreditorsInput(ChangeEventArgs e, int fieldToken)
        {
            if (Model == null) return;
            ClearValidationMessage();

            decimal.TryParse(e.Value?.ToString(), out decimal proposedValue);
            if (proposedValue < 0) proposedValue = 0;

            decimal sumOthers = 0;
            if (fieldToken != 60) sumOthers += Model.Creditors_60;
            if (fieldToken != 90) sumOthers += Model.Creditors_90;
            if (fieldToken != 120) sumOthers += Model.Creditors_120;
            if (fieldToken != 125) sumOthers += Model.Creditors_120Plus;

            if (sumOthers + proposedValue > 100m)
            {
                ValidationAlertMessage = "Entry Rejected: Combined Creditors aging allocation percentages cannot exceed 100%.";
                _Toast.ShowError("Allocation constraint ceiling overflow!");
                return;
            }

            switch (fieldToken)
            {
                case 60: Model.Creditors_60 = proposedValue; break;
                case 90: Model.Creditors_90 = proposedValue; break;
                case 120: Model.Creditors_120 = proposedValue; break;
                case 125: Model.Creditors_120Plus = proposedValue; break;
            }
            Model.Creditors_30 = CalculatedCreditors0To30;
        }

        private void InterceptDebtorsInput(ChangeEventArgs e, int fieldToken)
        {
            if (Model == null) return;
            ClearValidationMessage();

            decimal.TryParse(e.Value?.ToString(), out decimal proposedValue);
            if (proposedValue < 0) proposedValue = 0;

            decimal sumOthers = 0;
            if (fieldToken != 60) sumOthers += Model.Debtors_60;
            if (fieldToken != 90) sumOthers += Model.Debtors_90;
            if (fieldToken != 120) sumOthers += Model.Debtors_120;
            if (fieldToken != 125) sumOthers += Model.Debtors_120Plus;

            if (sumOthers + proposedValue > 100m)
            {
                ValidationAlertMessage = "Entry Rejected: Combined Debtors aging allocation percentages cannot exceed 100%.";
                _Toast.ShowError("Allocation constraint ceiling overflow!");
                return;
            }

            switch (fieldToken)
            {
                case 60: Model.Debtors_60 = proposedValue; break;
                case 90: Model.Debtors_90 = proposedValue; break;
                case 120: Model.Debtors_120 = proposedValue; break;
                case 125: Model.Debtors_120Plus = proposedValue; break;
            }
            Model.Debtors_30 = CalculatedDebtors0To30;
        }

        private void ClearValidationMessage() => ValidationAlertMessage = string.Empty;

        private async Task ExecuteGlobalSaveWorkflow()
        {
            if (Model == null || IsSubmitting) return;

            try
            {
                IsSubmitting = true;

                Model.Creditors_30 = CalculatedCreditors0To30;
                Model.Debtors_30 = CalculatedDebtors0To30;
                Model.MonthlyDirectorWagesAmountTotal = TotalCalculatedDirectorWages;

                bool success = await DataRepository.SaveAsync(Model);

                if (success)
                {
                    _Toast.ShowSuccess("Assessment parameters safely committed to the database ledger.");
                    ClearValidationMessage();
                }
                else
                {
                    _Toast.ShowError("The database transaction update statement was rejected.");
                }
            }
            catch (Exception ex)
            {
                _Toast.ShowError($"Exception logging transactional dataset save: {ex.Message}");
            }
            finally
            {
                IsSubmitting = false;
            }
        }

        private void OpenConfigurationPanel(string targetToken, long selectedId = 0)
        {
            ActiveFormType = null;
            ActiveFormParameters.Clear();

            switch (targetToken)
            {
                case "ASSESSMENT":
                    ActivePanelTitle = "Setup Corporate Profiles";
                    break;
                case "DEBTORS-CREDITORS":
                    ActivePanelTitle = "Map Debtors/Creditors Profiles";
                    break;

                case "SALES-CATEGORY":
                    ActivePanelTitle = selectedId > 0 ? "Edit Sales Category" : "Add New Sales Category";
                    ActiveFormType = typeof(SalesCategoryFormComponent);
                    ActiveFormParameters.Add("AssessmentSalesCategoryId", selectedId);
                    break;

                case "ASSESSMENT-LOANS":
                    ActivePanelTitle = selectedId > 0 ? "Edit Assessment Loan" : "Add New Assessment Loan";
                    ActiveFormType = typeof(AssessmentLoanFormComponent);
                    ActiveFormParameters.Add("AssessmentLoanId", selectedId);
                    break;
            }

            if (ActiveFormType != null)
            {
                ActiveFormParameters.Add("AssessmentId", ActiveAssessmentId);
                ActiveFormParameters.Add("OnSaveComplete", EventCallback.Factory.Create<SaveResult>(this, HandleFormExecutionCallback));
                OffCanvasControlRef?.OpenAsync();
            }
        }

        private async Task HandleFormExecutionCallback(SaveResult executionPackage)
        {
            if (SalesCategoryListRef != null)
            {
                // Forces the child list to execute a fresh database round-trip check instantly
                await SalesCategoryListRef.RefreshListAsync();
            }

            if (executionPackage == null) return;

            if (executionPackage.Success)
            {
                _Toast.ShowSuccess(executionPackage.Message);

                if (executionPackage.ClosePanel)
                {
                    OffCanvasControlRef?.CloseAsync();
                    ActiveFormType = null;
                }
                StateHasChanged();
            }
            else
            {
                _Toast.ShowError(executionPackage.Message ?? "An unexpected parameter verification crash occurred.");
            }
        }
    }
}