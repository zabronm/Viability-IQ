using Microsoft.AspNetCore.Components;
using ViabilityIQ.Application.Interfaces;
using ViabilityIQ.Infrastructure.Repositories;
using ViabilityIQ.Shared.DataModels;
using ViabilityIQ.Shared.SharedModels;
using Microsoft.Extensions.Logging;

namespace ViabilityIQ.Web.Components.Pages_Assessments.PageFormComponents.SettingsPageComponents
{
    public partial class DebtorsCreditorsComponent
    {
        [Inject] private ISessionService? sessionService { get; set; }
        [Inject] private IGenericDataRepository<DebtorsCreditorsProfile>? drCrGenericService { get; set; }
        [Inject] private MasterDataService? ViqCrudService { get; set; }
        [Inject] private ILogger<DebtorsCreditorsComponent>? Logger { get; set; }

        public DebtorsCreditorsProfile? debtorsCreditorsModel { get; set; }
        [Parameter] public long AssessmentId { get; set; }
        [Parameter] public EventCallback<SaveResult> OnUpdate { get; set; }

        private SaveResult result = new();
        private bool IsExecutionSuccess = false;
        private bool IsSubmitting = false;
        private bool loadingStateActive = false;

        protected override async Task OnParametersSetAsync() => await LoadDebtorsCreditorsAsync();

        public async Task RefreshAsync()
        {
            await LoadDebtorsCreditorsAsync();
            StateHasChanged();
        }

        private async Task LoadDebtorsCreditorsAsync()
        {
            loadingStateActive = true;
            try
            {
                var resultSet = await ViqCrudService!.GetSingleAsync<DebtorsCreditorsProfile>(
                      "tblAssessmentDebtorsCreditorsProfile",
                      new { AssessmentId });

                if (resultSet != null)
                {
                    debtorsCreditorsModel = resultSet;
                }
                else
                {
                    // Create new with defaults
                    debtorsCreditorsModel = new DebtorsCreditorsProfile
                    {
                        AssessmentId = AssessmentId,
                        Creditors_30 = 50m,
                        Creditors_60 = 20m,
                        Creditors_90 = 10m,
                        Creditors_120 = 10m,
                        Creditors_120Plus = 10m,
                        Debtors_30 = 50m,
                        Debtors_60 = 20m,
                        Debtors_90 = 10m,
                        Debtors_120 = 10m,
                        Debtors_120Plus = 10m,
                        EntryMode = false  // Default to percentage mode
                    };
                }

                Logger?.LogInformation("Loaded debtors/creditors profile for assessment {AssessmentId}", AssessmentId);
            }
            catch (Exception ex)
            {
                Logger?.LogError(ex, "Error loading debtors/creditors profile");
                debtorsCreditorsModel = new DebtorsCreditorsProfile { AssessmentId = AssessmentId };
            }
            finally
            {
                loadingStateActive = false;
                StateHasChanged();
            }
        }

        #region Calculated Properties

        /// <summary>
        /// Creditors 0-30 days is always the remainder from 100%
        /// </summary>
        private decimal CalculatedCreditors0To30
        {
            get
            {
                if (debtorsCreditorsModel == null)
                    return 50m;

                var sum = (debtorsCreditorsModel?.Creditors_60 ?? 0m) +
                         (debtorsCreditorsModel?.Creditors_90 ?? 0m) +
                         (debtorsCreditorsModel?.Creditors_120 ?? 0m) +
                         (debtorsCreditorsModel?.Creditors_120Plus ?? 0m);

                var remainder = 100m - sum;
                return remainder < 0m ? 0m : remainder;
            }
        }

        /// <summary>
        /// Calculated Creditors 0-30 Value value based on percentage
        /// </summary>
        private decimal CalculatedCreditors0To30Value
        {
            get
            {
                if (debtorsCreditorsModel == null)
                    return 0m;

                // Base value (usually the first creditor's Value value, typically 30-day terms)
                var baseValue = debtorsCreditorsModel?.CreditorsValue_30 ?? 0m;

                // If we have a reference month amount, calculate proportionally
                if (baseValue <= 0m)
                    return 0m;

                return (CalculatedCreditors0To30 / 100m) * baseValue;
            }
        }

        /// <summary>
        /// Debtors 0-30 days is always the remainder from 100%
        /// </summary>
        private decimal CalculatedDebtors0To30
        {
            get
            {
                if (debtorsCreditorsModel == null)
                    return 50m;

                var sum = (debtorsCreditorsModel?.Debtors_60 ?? 0m) +
                         (debtorsCreditorsModel?.Debtors_90 ?? 0m) +
                         (debtorsCreditorsModel?.Debtors_120 ?? 0m) +
                         (debtorsCreditorsModel?.Debtors_120Plus ?? 0m);

                var remainder = 100m - sum;
                return remainder < 0m ? 0m : remainder;
            }
        }

        /// <summary>
        /// Calculated Debtors 0-30 Value value based on percentage
        /// </summary>
        private decimal CalculatedDebtors0To30Value
        {
            get
            {
                if (debtorsCreditorsModel == null)
                    return 0m;

                // Base value (usually the first debtor's Value value, typically 30-day terms)
                var baseValue = debtorsCreditorsModel?.DebtorsValue_30 ?? 0m;

                // If we have a reference month amount, calculate proportionally
                if (baseValue <= 0m)
                    return 0m;

                return (CalculatedDebtors0To30 / 100m) * baseValue;
            }
        }

        /// <summary>
        /// Total all creditors percentages
        /// </summary>
        private decimal TotalCreditorsPercentage
        {
            get
            {
                if (debtorsCreditorsModel == null)
                    return 100m;

                return CalculatedCreditors0To30 +
                       (debtorsCreditorsModel?.Creditors_60 ?? 0m) +
                       (debtorsCreditorsModel?.Creditors_90 ?? 0m) +
                       (debtorsCreditorsModel?.Creditors_120 ?? 0m) +
                       (debtorsCreditorsModel?.Creditors_120Plus ?? 0m);
            }
        }

        /// <summary>
        /// Total all creditors values
        /// </summary>
        private decimal TotalCreditorsValue
        {
            get
            {
                if (debtorsCreditorsModel == null)
                    return 0m;

                return CalculatedCreditors0To30Value +
                       (debtorsCreditorsModel?.CreditorsValue_60 ?? 0m) +
                       (debtorsCreditorsModel?.CreditorsValue_90 ?? 0m) +
                       (debtorsCreditorsModel?.CreditorsValue_120 ?? 0m) +
                       (debtorsCreditorsModel?.CreditorsValue_120Plus ?? 0m);
            }
        }

        /// <summary>
        /// Total all debtors percentages
        /// </summary>
        private decimal TotalDebtorsPercentage
        {
            get
            {
                if (debtorsCreditorsModel == null)
                    return 100m;

                return CalculatedDebtors0To30 +
                       (debtorsCreditorsModel?.Debtors_60 ?? 0m) +
                       (debtorsCreditorsModel?.Debtors_90 ?? 0m) +
                       (debtorsCreditorsModel?.Debtors_120 ?? 0m) +
                       (debtorsCreditorsModel?.Debtors_120Plus ?? 0m);
            }                                
        }

        /// <summary>
        /// Total all debtors values
        /// </summary>
        private decimal TotalDebtorsValue
        {
            get
            {
                if (debtorsCreditorsModel == null)
                    return 0m;

                return CalculatedDebtors0To30Value +
                       (debtorsCreditorsModel?.DebtorsValue_60 ?? 0m) +
                       (debtorsCreditorsModel?.DebtorsValue_90 ?? 0m) +
                       (debtorsCreditorsModel?.DebtorsValue_120 ?? 0m) +
                       (debtorsCreditorsModel?.DebtorsValue_120Plus ?? 0m);
            }
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// Handle entry mode toggle
        /// </summary>
        private async Task HandleEntryModeChange(ChangeEventArgs e)
        {
            if (debtorsCreditorsModel == null)
                return;

            debtorsCreditorsModel.EntryMode = (bool?)e.Value ?? false;
            StateHasChanged();
            await Task.CompletedTask;
        }

        /// <summary>
        /// Handle creditors percentage changes
        /// Auto-update the 0-30 day percentage to keep total at 100%
        /// </summary>
        private async Task HandleCreditorsPercentageChange()
        {
            if (debtorsCreditorsModel == null)
                return;

            // Ensure percentages don't exceed their limits
            debtorsCreditorsModel.Creditors_60 = Math.Max(0m, Math.Min(100m, debtorsCreditorsModel?.Creditors_60 ?? 0m));
            debtorsCreditorsModel.Creditors_90 = Math.Max(0m, Math.Min(100m, debtorsCreditorsModel?.Creditors_90 ?? 0m));
            debtorsCreditorsModel.Creditors_120 = Math.Max(0m, Math.Min(100m, debtorsCreditorsModel?.Creditors_120 ?? 0m));
            debtorsCreditorsModel.Creditors_120Plus = Math.Max(0m, Math.Min(100m, debtorsCreditorsModel?.Creditors_120Plus ?? 0m));

            // Auto-update 0-30 to maintain 100% total
            debtorsCreditorsModel.Creditors_30 = CalculatedCreditors0To30;

            StateHasChanged();
            await Task.CompletedTask;
        }

        /// <summary>
        /// Handle creditors Value value changes
        /// </summary>
        private async Task HandleCreditorsValueChange()
        {
            if (debtorsCreditorsModel == null)
                return;

            // Ensure values are not negative
            debtorsCreditorsModel.CreditorsValue_60 = Math.Max(0m, debtorsCreditorsModel?.CreditorsValue_60 ?? 0m);
            debtorsCreditorsModel.CreditorsValue_90 = Math.Max(0m, debtorsCreditorsModel?.CreditorsValue_90 ?? 0m);
            debtorsCreditorsModel.CreditorsValue_120 = Math.Max(0m, debtorsCreditorsModel?.CreditorsValue_120 ?? 0m);
            debtorsCreditorsModel.CreditorsValue_120Plus = Math.Max(0m, debtorsCreditorsModel?.CreditorsValue_120Plus ?? 0m);

            StateHasChanged();
            await Task.CompletedTask;
        }

        /// <summary>
        /// Handle debtors percentage changes
        /// Auto-update the 0-30 day percentage to keep total at 100%
        /// </summary>
        private async Task HandleDebtorsPercentageChange()
        {
            if (debtorsCreditorsModel == null)
                return;

            // Ensure percentages don't exceed their limits
            debtorsCreditorsModel.Debtors_60 = Math.Max(0m, Math.Min(100m, debtorsCreditorsModel?.Debtors_60 ?? 0m));
            debtorsCreditorsModel.Debtors_90 = Math.Max(0m, Math.Min(100m, debtorsCreditorsModel?.Debtors_90 ?? 0m));
            debtorsCreditorsModel.Debtors_120 = Math.Max(0m, Math.Min(100m, debtorsCreditorsModel?.Debtors_120 ?? 0m));
            debtorsCreditorsModel.Debtors_120Plus = Math.Max(0m, Math.Min(100m, debtorsCreditorsModel?.Debtors_120Plus ?? 0m));

            // Auto-update 0-30 to maintain 100% total
            debtorsCreditorsModel.Debtors_30 = CalculatedDebtors0To30;

            StateHasChanged();
            await Task.CompletedTask;
        }

        /// <summary>
        /// Handle debtors Value value changes
        /// </summary>
        private async Task HandleDebtorsValueChange()
        {
            if (debtorsCreditorsModel == null)
                return;

            // Ensure values are not negative
            debtorsCreditorsModel.DebtorsValue_60 = Math.Max(0m, debtorsCreditorsModel?.DebtorsValue_60 ?? 0m);
            debtorsCreditorsModel.DebtorsValue_90 = Math.Max(0m, debtorsCreditorsModel?.DebtorsValue_90 ?? 0m);
            debtorsCreditorsModel.DebtorsValue_120 = Math.Max(0m, debtorsCreditorsModel?.DebtorsValue_120 ?? 0m);
            debtorsCreditorsModel.DebtorsValue_120Plus = Math.Max(0m, debtorsCreditorsModel?.DebtorsValue_120Plus ?? 0m);

            StateHasChanged();
            await Task.CompletedTask;
        }

        #endregion

        async Task UpdateDebtorsCreditors()
        {
            if (debtorsCreditorsModel == null)
                return;

            try
            {
                IsSubmitting = true;

                // Final sync of calculated values before submission
                debtorsCreditorsModel.Creditors_30 = CalculatedCreditors0To30;
                debtorsCreditorsModel.CreditorsValue_30 = CalculatedCreditors0To30Value;
                debtorsCreditorsModel.Debtors_30 = CalculatedDebtors0To30;
                debtorsCreditorsModel.DebtorsValue_30 = CalculatedDebtors0To30Value;

                IsExecutionSuccess = await drCrGenericService.SaveAsync(debtorsCreditorsModel);

                if (IsExecutionSuccess)
                {
                    result = SaveResult.SavedAndClose("Debtors/Creditor profiles updated successfully.");
                    Logger?.LogInformation("Debtors/Creditors profile saved for assessment {AssessmentId}", AssessmentId);
                }
                else
                {
                    result = SaveResult.Failed("Error encountered, could not save/update debtor/creditor profiles. Please retry.");
                    Logger?.LogWarning("Failed to save debtors/creditors profile for assessment {AssessmentId}", AssessmentId);
                }
            }
            catch (Exception ex)
            {
                result = new()
                {
                    Message = $"Error encountered: {ex.Message}",
                    Success = false,
                };
                Logger?.LogError(ex, "Error updating debtors/creditors profile");
            }
            finally
            {
                IsSubmitting = false;
                if (OnUpdate.HasDelegate)
                {
                    await OnUpdate.InvokeAsync(result);
                }
            }
        }
    }
}