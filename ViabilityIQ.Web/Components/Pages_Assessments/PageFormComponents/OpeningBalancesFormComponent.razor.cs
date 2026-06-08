using Microsoft.AspNetCore.Components;
using ViabilityIQ.Shared.SharedModels;

namespace ViabilityIQ.Web.Components.Pages_Assessments.PageFormComponents
{
    public partial class OpeningBalancesFormComponent
    {
        [Parameter] public long AssessmentId { get; set; }
        [Parameter] public EventCallback<SaveResult> OnSaveComplete { get; set; }


        private async Task HandleFormSubmit()
        {
            try
            {
                bool isSaveSuccessful = true;
                var result = new SaveResult
                {
                    Success = isSaveSuccessful,
                    ClosePanel = true,
                    Message = "Sales categories successfully updated into ledger state."
                };

                await OnSaveComplete.InvokeAsync(result);
            }
            catch (Exception ex)
            {

                await OnSaveComplete.InvokeAsync(new SaveResult
                {
                    Success = false,
                    Message = $"Save aborted: {ex.Message}"
                });
            }
        }
    }
}
