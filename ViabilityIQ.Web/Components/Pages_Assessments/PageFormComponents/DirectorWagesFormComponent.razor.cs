using Microsoft.AspNetCore.Components;
using ViabilityIQ.Application.Interfaces;
using ViabilityIQ.Shared.DataModels;
using ViabilityIQ.Shared.SharedModels;

namespace ViabilityIQ.Web.Components.Pages_Assessments.PageFormComponents
{
    public partial class DirectorWagesFormComponent : ComponentBase
    {
        [Parameter] public long AssessmentId { get; set; }
        [Parameter] public EventCallback<SaveResult> OnSaveComplete { get; set; }

        // Injecting your standard repository engine interface signature
        [Inject] private IGenericDataRepository<DirectorWages> DataRepository { get; set; } = default!;

        private DirectorWages? Model { get; set; }
        private bool IsLoading { get; set; } = true;
        private bool IsSubmitting { get; set; } = false;

        protected override async Task OnInitializedAsync()
        {
            await HydrateFormStateDataAsync();
        }

        private async Task HydrateFormStateDataAsync()
        {
            try
            {
                IsLoading = true;

                // 1. Query table parameters from database context
                var tableRecords = await DataRepository.GetAllAsync();

                // Locate the exact single record bound to this assessment context profile scope
                var isolatedRecord = tableRecords?.FirstOrDefault(x => x.AssessmentId == AssessmentId);

                if (isolatedRecord != null)
                {
                    Model = isolatedRecord;
                }
                else
                {
                    // 2. Initialize a clean data frame with DirectorWagesId = 0 to trigger an implicit INSERT
                    Model = new DirectorWages
                    {
                        DirectorWagesId = 0,
                        AssessmentId = AssessmentId,
                        NumberOfDirectors = 1,
                        MonthlyDirectorWagesAmount = 0,
                        MonthlyDirectorWagesAmountTotal = 0,
                        Active = true,
                        Remarks = string.Empty,
                        CreatedDate = DateTime.UtcNow,
                        ModifiedDate = DateTime.UtcNow
                    };
                }
            }
            catch (Exception ex)
            {
                await OnSaveComplete.InvokeAsync(new SaveResult
                {
                    Success = false,
                    ClosePanel = false,
                    Message = $"Initialization error mapping data: {ex.Message}"
                });
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void CalculateWagesImpact()
        {
            if (Model == null) return;

            // Maintain precision calculations inside active RAM boundaries
            Model.MonthlyDirectorWagesAmountTotal = Model.NumberOfDirectors * Model.MonthlyDirectorWagesAmount;
        }

        private async Task HandleFormSubmit()
        {
            if (Model == null || IsSubmitting) return;

            try
            {
                IsSubmitting = true;
                CalculateWagesImpact();

                // 3. Persist modifications using your standard GenericDataRepository wrapper.
                // Because Model.DirectorWagesId cleanly tracks 0 for inserts and > 0 for updates,
                // your implementation handles all internal audit-property injections automatically!
                bool isExecutionSuccess = await DataRepository.SaveAsync(Model);

                var feedbackMessagePackage = new SaveResult
                {
                    Success = isExecutionSuccess,
                    ClosePanel = isExecutionSuccess,
                    Message = isExecutionSuccess
                        ? "Director wages configuration saved successfully."
                        : "The transactional buffer packet write command was rejected by the database engine."
                };

                await OnSaveComplete.InvokeAsync(feedbackMessagePackage);
            }
            catch (Exception ex)
            {
                await OnSaveComplete.InvokeAsync(new SaveResult
                {
                    Success = false,
                    ClosePanel = false,
                    Message = $"Critical exception committing persistence streams: {ex.Message}"
                });
            }
            finally
            {
                IsSubmitting = false;
            }
        }
    }
}