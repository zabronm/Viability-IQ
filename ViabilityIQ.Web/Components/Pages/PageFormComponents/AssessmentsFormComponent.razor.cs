using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
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

        private string formattedStartDate = DateTime.Today.ToString("yyyy-MM-dd");
        private string formattedEndDate = DateTime.Today.AddMonths(1).ToString("yyyy-MM-dd");

        private bool boolStock { get; set; }
        private bool boolDebtors { get; set; }
        private bool boolExpenses { get; set; }
        private bool boolSales { get; set; }
        private bool boolVat { get; set; }

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
            if (assessmentModel.BusinessId == 0) return;

            isProcessingData = true;
            var finalResult = new SaveResult();

            try
            {
                assessmentModel.blStock = boolStock ? 1 : 0;
                assessmentModel.blDebtorsCreditors = boolDebtors ? 1 : 0;
                assessmentModel.blExpenses = boolExpenses ? 1 : 0;
                assessmentModel.blSales = boolSales ? 1 : 0;
                assessmentModel.blVat = boolVat ? 1 : 0;

                bool operationSuccess = await assessmentRepository.SaveAsync(assessmentModel);

                if (operationSuccess)
                {
                    finalResult.Success = true;
                    finalResult.ClosePanel = true;
                    finalResult.Message = AssessmentId == 0
                        ? "New assessment successfully deployed."
                        : "Assessment modified successfully.";
                }
                else
                {
                    finalResult.Success = false;
                    finalResult.ClosePanel = false;
                    finalResult.Message = "The persistence context engine returned false. Transaction aborted.";
                }

                // ✅ Use service to publish result
                await OffcanvasService!.PublishResultAsync(finalResult);
            }
            catch (Exception ex)
            {
                finalResult.Success = false;
                finalResult.ClosePanel = false;
                finalResult.Message = $"Pipeline Intercepted Failure: {ex.Message}";

                await OffcanvasService!.PublishResultAsync(finalResult);
            }
            finally
            {
                isProcessingData = false;
            }
        }
    }
}