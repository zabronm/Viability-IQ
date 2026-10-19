using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq.Expressions;
using System.Threading.Tasks;
using ViabilityIQ.Application.Interfaces;
using ViabilityIQ.Shared.DataModels;
using ViabilityIQ.Shared.SharedModels;
using ViabilityIQ.Web.Services;

namespace ViabilityIQ.Web.Components.Pages.PageFormComponents
{
    public partial class AssessmentsFormComponent
    {
        [Inject] private IGenericDataRepository<Assessment> assessmentRepository { get; set; } = default!;
        [Inject] private IGenericDataRepository<BusinessDto> businessLookupRepository { get; set; } = default!;
        [Inject] private OffCanvasStateService? OffcanvasService { get; set; } = default!;  // ✅ ADD THIS

        [Parameter] public long AssessmentId { get; set; } = 0;

        private Assessment assessmentModel = new();
        private Dictionary<long, string> activeBusinessLookup = new();
        private bool isProcessingData = false;
        private string? formError;

        private string formattedStartDate = DateTime.Today.ToString("yyyy-MM-dd");
        private string formattedEndDate = DateTime.Today.AddMonths(1).ToString("yyyy-MM-dd");
        private string LocaleDateSample =>
            DateTime.Today.ToString(GetCulture().DateTimeFormat.ShortDatePattern, GetCulture());
        private string LocaleNumberSample => 1234567.89m.ToString("N2", GetCulture());

        private bool isRowActive = true;
        private bool boolStock { get; set; }
        private bool boolDebtors { get; set; }
        private bool boolExpenses { get; set; }
        private bool boolSales { get; set; }
        private bool boolVat { get; set; }

        private CultureInfo GetCulture()
        {
            try
            {
                return CultureInfo.GetCultureInfo(assessmentModel.CultureName);
            }
            catch (CultureNotFoundException)
            {
                return CultureInfo.GetCultureInfo("en-ZA");
            }
        }

        private void OnCultureChanged(string? cultureName)
        {
            var locale = AssessmentLocaleOptions.EnglishCultures
                .FirstOrDefault(item => item.CultureName == cultureName);
            if (locale is null)
                return;

            assessmentModel.CultureName = locale.CultureName;
            assessmentModel.CountryCode = locale.CountryCode;
            assessmentModel.CurrencyCode = locale.DefaultCurrencyCode;
        }

        protected override async Task OnInitializedAsync()
        {
            try
            {
                var businesses = await businessLookupRepository.GetAllAsync();
                if (businesses != null)
                {
                    activeBusinessLookup = businesses.ToDictionary(x => x.BusinessId, x => x.BusinessName ?? $"ID: {x.BusinessId}");
                }

            }
            catch { /* Graceful configuration fallback */ }
        }

        protected override async Task OnParametersSetAsync()
        {
            formError = null;
            if (AssessmentId != 0)
            {
                isProcessingData = true;
                try
                {
                    var record = await assessmentRepository.GetByIdAsync(AssessmentId);
                    if (record != null)
                    {
                        assessmentModel = record;
                        formattedStartDate = assessmentModel!.AssessmentStartDate?.ToString("yyyy-MM-dd");
                        formattedEndDate = assessmentModel.AssessmentFinishDate?.ToString("yyyy-MM-dd");

                        boolStock = assessmentModel.blStock == 1;
                        boolDebtors = assessmentModel.blDebtorsCreditors == 1;
                        boolExpenses = assessmentModel.blExpenses == 1;
                        boolSales = assessmentModel.blSales == 1;
                        boolVat = assessmentModel.blVat == 1;                        
                    }
                }
                catch (Exception ex)
                {
                    // Handle exception
                }
                finally
                {
                    isProcessingData = false;
                }
            }
            else
            {
                assessmentModel = new Assessment
                {
                    StatusId = 1,
                    AssessmentTypeId = 1,
                    AssessmentModeId = (int)AssessmentMode.Trading,
                    Active = true
                };

                formattedStartDate = DateTime.Today.ToString("yyyy-MM-dd");
                formattedEndDate = DateTime.Today.AddMonths(3).ToString("yyyy-MM-dd");

                boolStock = boolDebtors = boolExpenses = boolSales = boolVat = true;
            }
        }

        private void OnStartDateChanged(ChangeEventArgs e)
        {
            if (DateTime.TryParse(e.Value?.ToString(), out var dt))
            {
                formattedStartDate = dt.ToString("yyyy-MM-dd");
                assessmentModel.AssessmentStartDate = dt;
            }
        }

        private void OnEndDateChanged(ChangeEventArgs e)
        {
            if (DateTime.TryParse(e.Value?.ToString(), out var dt))
            {
                formattedEndDate = dt.ToString("yyyy-MM-dd");
                assessmentModel.AssessmentFinishDate = dt;
            }
        }

        protected async Task HandleFormSubmissionAsync()
        {
            formError = null;
            if (assessmentModel.BusinessId <= 0)
            {
                formError = "Select an active business before saving the assessment.";
                return;
            }

            isProcessingData = true;
            var finalResult = new SaveResult();
            assessmentModel.Active = isRowActive;

            try
            {
                var isNewRecord = AssessmentId == 0;
                assessmentModel.blStock = boolStock ? 1 : 0;
                assessmentModel.blDebtorsCreditors = boolDebtors ? 1 : 0;
                assessmentModel.blExpenses = boolExpenses ? 1 : 0;
                assessmentModel.blSales = boolSales ? 1 : 0;
                assessmentModel.blVat = boolVat ? 1 : 0;
                assessmentModel.AssessmentTypeId = 4;               //============  This field is redundant, value only for FK constraint compatibility ==============

                bool operationSuccess = await assessmentRepository.SaveAsync(assessmentModel);

                if (operationSuccess)
                {
                    finalResult = isNewRecord
                        ? SaveResult.SavedAndNew(assessmentModel, "New assessment successfully deployed.")
                        : SaveResult.SavedAndClose(assessmentModel, "Assessment modified successfully.");
                }
                else
                {
                    finalResult.Success = false;
                    finalResult.ClosePanel = false;
                    finalResult.Message = "The persistence context engine returned false. Transaction aborted.";
                }

                // ✅ Use service to publish result
                await OffcanvasService!.PublishResultAsync(finalResult);

                if (finalResult.ClearForm)
                {
                    ResetForm();
                }
            }
            catch (Exception ex)
            {
                var message = $"Unable to save the assessment: {ex.Message}";
                formError = message;
                finalResult.Success = false;
                finalResult.ClosePanel = false;
                finalResult.Message = message;

                await OffcanvasService!.PublishResultAsync(finalResult);
            }
            finally
            {
                isProcessingData = false;
            }
        }

        private void ResetForm()
        {
            assessmentModel = new Assessment
            {
                StatusId = 1,
                AssessmentTypeId = 4,                           //============  This field is redundant, value only for FK constraint compatibility ==============
                AssessmentModeId = (int)AssessmentMode.Trading,
                Active = true
            };
            formattedStartDate = DateTime.Today.ToString("yyyy-MM-dd");
            formattedEndDate = DateTime.Today.AddMonths(3).ToString("yyyy-MM-dd");
            isRowActive = true;
            boolStock = boolDebtors = boolExpenses = boolSales = boolVat = true;
        }
    }
}