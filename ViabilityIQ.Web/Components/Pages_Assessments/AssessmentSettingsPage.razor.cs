using Microsoft.AspNetCore.Components;
using OfficeOpenXml.Data.Connection;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using ViabilityIQ.Application.Interfaces;
using ViabilityIQ.Infrastructure.Repositories;
using ViabilityIQ.Shared.DataModels;
using ViabilityIQ.Shared.SharedModels;
using ViabilityIQ.Web.Components.CommonComponents;
using ViabilityIQ.Web.Components.Pages_Assessments.PageFormComponents;
using ViabilityIQ.Web.Services;
using ViabilityIQ.Web.Components.Pages_Assessments.PageFormComponents.SettingsPageComponents;


namespace ViabilityIQ.Web.Components.Pages_Assessments
{
    public partial class AssessmentSettingsPage
    {
        [Inject] private ISessionService? sessionService { get; set; }
        [Inject] private ZabOffCanvasService zabOffCanvasService { get; set; } = default!;
        [Inject] private ToastService _Toast { get; set; } = default!;
        [Inject] private IGenericDataRepository<Assessment> DataRepository { get; set; } = default!;
       

        [Parameter] public long ActiveAssessmentId { get; set; }

        private SalesCategoryListComponent? SalesCategoryListRef;
        private AssessmentLoansListComponent? AssessmentLoansListRef;

        private Assessment? Model { get; set; }

        //=========== INDEPENDENT VARIABLES      
        VatUpdateModel? vatUpdateModel { get; set; }
        DirectorWagesModel? directorWagesModel { get; set; }
        //DebtorsCreditorsModel? debtorsCreditorsModel { get; set; }
        OpeningBalancesModel? openingBalancesModel { get; set; }


        private bool IsLoading { get; set; } = true;
        private bool IsSubmitting { get; set; } = false;
        private string ValidationAlertMessage { get; set; } = string.Empty;

        private ZabOffCanvas? OffCanvasControlRef { get; set; }
        private string ActivePanelTitle { get; set; } = string.Empty;
        private Type? ActiveFormType { get; set; }
        private Dictionary<string, object> ActiveFormParameters { get; set; } = new();

        private string? str_sql;                

        protected override async Task OnInitializedAsync()
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
                    PopulateDataModels();           //populate VAT model
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


        void PopulateDataModels()
        {
            //============  VATRate model
            vatUpdateModel = new()                 
            {
                AssessmentId = Model.AssessmentId,
                CaseNumber = Model.CaseNumber,
                BusinessId = Model.BusinessId,
                AssessmentFinishDate = Model.AssessmentFinishDate,
                AssessmentStartDate = Model.AssessmentStartDate,
                VatRate =  Model.VATRate,
            };

            //==========   Opening Balances
            openingBalancesModel = new()
            {
                AssessmentId = Model.AssessmentId,
                OpeningBalance_Assets = Model.OpeningBalance_Assets,
                OpeningBalance_Bank = Model.OpeningBalance_Bank,
                InterestOnOverDraft = Model.InterestOnOverDraft,
                DepreciationProjectedAmount = Model.DepreciationProjectedAmount
            };
                     

            //==========   Director Wages
            directorWagesModel = new()
            {
                AssessmentId = Model.AssessmentId,
                MonthlyDirectorWagesAmount = Model.MonthlyDirectorWagesAmount,
                NumberOfDirectors = Model.NumberOfDirectors,
                MonthlyDirectorWagesAmountTotal = Model.MonthlyDirectorWagesAmount * Model.NumberOfDirectors
            };
        }


        private void ClearValidationMessage() => ValidationAlertMessage = string.Empty;

        private async Task ExecuteGlobalSaveWorkflow()
        {
            if (Model == null || IsSubmitting) return;

            try
            {
                IsSubmitting = true;

                //Model.Creditors_30 = CalculatedCreditors0To30;
                //Model.Debtors_30 = CalculatedDebtors0To30;
                //Model.MonthlyDirectorWagesAmountTotal = TotalCalculatedDirectorWages;

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

        private async Task OpenConfigurationPanel(string targetToken, long selectedId = 0)
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

                    ActivePanelTitle = selectedId == 0 ?
                    "Add Sales Category" : "Edit Sales Category";

                    await zabOffCanvasService.ShowAsync(
                        new CanvasRequest
                        {
                            Title = ActivePanelTitle,
                            Width = 350,
                            ComponentType = typeof(SalesCategoryFormComponent),
                            Parameters = new
                            {
                                AssessmentSalesCategoryId = selectedId,
                                AssessmentId = ActiveAssessmentId,
                            }
                        });

                    break;


                case "ASSESSMENT-LOANS":

                    ActivePanelTitle = selectedId > 0 ? "Edit Assessment Loan" : "Add New Assessment Loan";
                    await zabOffCanvasService.ShowAsync(
                        new CanvasRequest
                        {
                            Title = ActivePanelTitle,
                            Width = 365,
                            ComponentType = typeof(AssessmentLoanFormComponent),
                            Parameters = new
                            {
                                AssessmentLoanId = 0L,                      //INDICATE LONHG INTERGER WHICH PARAMETER EXPECTS ===========>
                                AssessmentId = ActiveAssessmentId,
                            }
                        });

                    break;
            }

            if (ActiveFormType != null)
            {
                ActiveFormParameters.Add("AssessmentId", ActiveAssessmentId);
                ActiveFormParameters.Add("OnSaveComplete", EventCallback.Factory.Create<SaveResult>(this, HandleFormExecutionCallback));
                OffCanvasControlRef?.OpenAsync();
            }
        }


        //============================== REFEESH AFTER UPDATING ANY COMPONENT ============
        private async Task RefreshComponentData(SaveResult saveResult)
        {
            if (saveResult.Success)
            {
                _Toast.ShowSuccess(saveResult.Message, sessionService!.AppTitle);
                await HydrateAssessmentRecordAsync();
                PopulateDataModels();
                StateHasChanged();
            }
            else
            {
                _Toast.ShowError(saveResult.Message, sessionService!.AppTitle);
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


    public class VatUpdateModel
    {
        public long AssessmentId { get; set; }
        public string? CaseNumber { get; set; }
        public DateTime? AssessmentStartDate { get; set; }
        public DateTime? AssessmentFinishDate { get; set; }
        public long BusinessId { get; set; }
        [Required(ErrorMessage = "VAT is required, use zero(o) if exempted.")]
        [Range(0, 16.99, ErrorMessage = "VAT must be between 0 and 17.")]
        public decimal VatRate { get; set; }
    }

    public class DirectorWagesModel
    {
        public long AssessmentId { get; set; }
        [Required(ErrorMessage = "At least one director is required.")]
        [Range(1, long.MaxValue, ErrorMessage = "There must be at least 1 Director.")]
        public decimal MonthlyDirectorWagesAmountTotal { get; set; }

        [Required(ErrorMessage = "Director wage is required.")]
        [Range(1, long.MaxValue, ErrorMessage = "There must be at least 1 Director wage.")]
        public decimal MonthlyDirectorWagesAmount { get; set; }
        public int NumberOfDirectors { get; set; }
    }
          

    public class OpeningBalancesModel
    {
        public long AssessmentId { get; set; }
        public decimal OpeningBalance_Assets { get; set; }
        public decimal OpeningBalance_Bank { get; set; }
        public decimal InterestOnOverDraft { get; set; } = 0;
        public decimal DepreciationProjectedAmount { get; set; } = 0;
    }

}