using Microsoft.AspNetCore.Components;
using ViabilityIQ.Application.Interfaces;
using ViabilityIQ.Infrastructure.Repositories;
using ViabilityIQ.Shared.DataModels;
using ViabilityIQ.Shared.SharedModels;

namespace ViabilityIQ.Web.Components.Pages_Assessments.PageFormComponents.SettingsPageComponents
{
    public partial class DebtorsCreditorsComponent
    {
        [Inject] private ISessionService? sessionService { get; set; }
        [Inject] private MasterDataService? ViqCrudService { get; set; }

        public DebtorsCreditorsProfile? debtorsCreditorsModel { get; set; }
        [Parameter] public long AssessmentId { get; set; }
        [Parameter] public EventCallback<SaveResult> OnUpdate { get; set; }

        private SaveResult result = new();
        private bool IsSubmitting = false;
        private bool loadingStateActive = false;

        // Centralized logic for UI state[cite: 2]
        private bool IsPercentageReadonly => debtorsCreditorsModel?.EntryMode ?? false;
        private bool IsValueReadonly => !(debtorsCreditorsModel?.EntryMode ?? false);

        private decimal CalculatedCreditors0To30 => Math.Max(0m, 100m - ((debtorsCreditorsModel?.Creditors_60 ?? 0m) + (debtorsCreditorsModel?.Creditors_90 ?? 0m) + (debtorsCreditorsModel?.Creditors_120 ?? 0m) + (debtorsCreditorsModel?.Creditors_120Plus ?? 0m)));
        private decimal CalculatedDebtors0To30 => Math.Max(0m, 100m - ((debtorsCreditorsModel?.Debtors_60 ?? 0m) + (debtorsCreditorsModel?.Debtors_90 ?? 0m) + (debtorsCreditorsModel?.Debtors_120 ?? 0m) + (debtorsCreditorsModel?.Debtors_120Plus ?? 0m)));

        protected override async Task OnParametersSetAsync() => await LoadDebtorsCreditorsAsync();

        private async Task LoadDebtorsCreditorsAsync()
        {
            loadingStateActive = true;
            try
            {
                var resultSet = await ViqCrudService!.GetSingleAsync<DebtorsCreditorsProfile>(
                      "tblAssessmentWorkingCapitalProfile",
                      new { AssessmentId, EntryMode = false });
                debtorsCreditorsModel = resultSet ?? new DebtorsCreditorsProfile();
            }
            finally
            {
                loadingStateActive = false;
                StateHasChanged();
            }
        }

        // Methods now accept both arguments to match the Razor lambdas[cite: 2]
        public void InterceptCreditorsInput(ChangeEventArgs e, int fieldToken)
        {
            if (debtorsCreditorsModel == null) return;
            decimal.TryParse(e.Value?.ToString(), out decimal proposedValue);

            // Logic to update model fields based on fieldToken...
            // Ensure this logic is consistent with your existing implementation
            debtorsCreditorsModel.Creditors_30 = CalculatedCreditors0To30;
        }

        public void InterceptDebtorsInput(ChangeEventArgs e, int fieldToken)
        {
            if (debtorsCreditorsModel == null) return;
            decimal.TryParse(e.Value?.ToString(), out decimal proposedValue);

            // Logic to update model fields based on fieldToken...
            // Ensure this logic is consistent with your existing implementation
            debtorsCreditorsModel.Debtors_30 = CalculatedDebtors0To30;
        }

        async Task UpdateDebtorsCreditors()
        {
            /* Your existing SQL and execution logic here */
        }
    }
}