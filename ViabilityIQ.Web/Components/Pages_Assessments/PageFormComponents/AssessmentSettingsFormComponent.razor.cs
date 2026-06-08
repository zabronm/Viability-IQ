using Microsoft.AspNetCore.Components;
using ViabilityIQ.Application.Interfaces;
using ViabilityIQ.Shared.DataModels;
using ViabilityIQ.Shared.SharedModels;

namespace ViabilityIQ.Web.Components.Pages_Assessments.PageFormComponents
{
    public partial class AssessmentSettingsFormComponent : ComponentBase
    {
        [Parameter] public long AssessmentId { get; set; }
        [Parameter] public EventCallback<SaveResult> OnSaveComplete { get; set; }
        [Inject] private IGenericDataRepository<Assessment> DataRepository { get; set; } = default!;

        private Assessment? Model { get; set; }
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

                // Load the existing assessment directly by its primary identifier key
                var assessmentRecord = await DataRepository.GetByIdAsync(AssessmentId);

                if (assessmentRecord != null)
                {
                    Model = assessmentRecord;
                }
                else
                {
                    // Escape path safety if no assessment record could be resolved
                    await OnSaveComplete.InvokeAsync(new SaveResult
                    {
                        Success = false,
                        ClosePanel = false,
                        Message = $"Critical Error: Assessment identity context #{AssessmentId} was not found in the database layer."
                    });
                }
            }
            catch (Exception ex)
            {
                await OnSaveComplete.InvokeAsync(new SaveResult
                {
                    Success = false,
                    ClosePanel = false,
                    Message = $"Initialization error locating data parameters: {ex.Message}"
                });
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task ExecuteSaveWorkflow()
        {
            if (Model == null || IsSubmitting) return;

            try
            {
                IsSubmitting = true;

                // Persist modifications through the generic data repository pattern
                // Since AssessmentId > 0, the repository automatically triggers connection.UpdateAsync()
                bool isExecutionSuccess = await DataRepository.SaveAsync(Model);

                var feedbackPackage = new SaveResult
                {
                    Success = isExecutionSuccess,
                    ClosePanel = isExecutionSuccess,
                    Message = isExecutionSuccess
                        ? "Assessment parameters modified successfully."
                        : "The transactional buffer packet command was rejected."
                };

                await OnSaveComplete.InvokeAsync(feedbackPackage);
            }
            catch (Exception ex)
            {
                await OnSaveComplete.InvokeAsync(new SaveResult
                {
                    Success = false,
                    ClosePanel = false,
                    Message = $"Critical exception committing persistence workflow streams: {ex.Message}"
                });
            }
            finally
            {
                IsSubmitting = false;
            }
        }
    }
}