using Microsoft.AspNetCore.Components;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using ViabilityIQ.Application.Interfaces;
using ViabilityIQ.Shared.SharedModels;
using ViabilityIQ.Web.Components.CommonComponents;
using ViabilityIQ.Web.Components.Pages_Assessments.PageFormComponents;
using ViabilityIQ.Web.Services;

namespace ViabilityIQ.Web.Components.Pages_Assessments
{
    public partial class AssessmentSettingsPage
    {
        [Inject] ISessionService? sessionService { get; set; }
        [Inject] private ToastService _Toast { get; set; } = default!;
        [Parameter] public long ActiveAssessmentId { get; set; } 
        private ZabOffCanvas? OffCanvasControlRef { get; set; }
        private string ActivePanelTitle { get; set; } = string.Empty;
       
        private Type? ActiveFormType { get; set; }
        private Dictionary<string, object> ActiveFormParameters { get; set; } = new();
    

        protected override async Task OnParametersSetAsync()
        {
            ActiveAssessmentId = sessionService!.AssessmentId!.Value;
            await Task.CompletedTask;
        }

        private void OpenConfigurationPanel(string targetToken)
        {
            // Flush structural instances to prevent cross-contamination parameter exceptions
            ActiveFormType = null;
            ActiveFormParameters.Clear();

            switch (targetToken)
            {
                case "ASSESSMENT":
                    ActivePanelTitle = "Modify Assessment Parameters";
                    ActiveFormType = typeof(AssessmentSettingsFormComponent);
                    break;

                case "EXPENSES":
                    ActivePanelTitle = "Select Assessment Expense Items";
                    ActiveFormType = typeof(AssExpenseItemsFormComponent);
                    break;

                case "WAGES":
                    ActivePanelTitle = "Specify Director Wages";
                    ActiveFormType = typeof(DirectorWagesFormComponent);
                    break;

                case "SALES":
                    ActivePanelTitle = "Setup Sales Categories";
                    ActiveFormType = typeof(SalesCategoryFormComponent);
                    break;

                case "DEBTORS-CREDITORS":
                    ActivePanelTitle = "Setup Debtors/Creditors Profile";
                    ActiveFormType = typeof(DebtorsCreditorsFormComponent);
                    break;

                case "BALANCES":
                    ActivePanelTitle = "Setup Opening Balances";
                    ActiveFormType = typeof(OpeningBalancesFormComponent);
                    break;

            }

            if (ActiveFormType != null)
            {
                // Bind context parameters using exact string matching rules
                ActiveFormParameters.Add("AssessmentId", ActiveAssessmentId);

                // Map common runtime parameter key delegate
                ActiveFormParameters.Add("OnSaveComplete", EventCallback.Factory.Create<SaveResult>(this, HandleFormExecutionCallback));

                OffCanvasControlRef?.OpenAsync();
            }
        }

        private void HandleFormExecutionCallback(SaveResult executionPackage)
        {
            if (executionPackage == null) return;

            if (executionPackage.Success)
            {
                _Toast.ShowSuccess(executionPackage.Message);

                if (executionPackage.ClosePanel)
                {
                    OffCanvasControlRef?.CloseAsync();
                    ActiveFormType = null; // Purge layout memory space immediately
                }

                // Re-render UI list layout grids automatically
                StateHasChanged();
            }
            else
            {
                _Toast.ShowError(executionPackage.Message ?? "An unexpected parameter verification crash was logged.");
            }
        }

    }
}