using Microsoft.AspNetCore.Components;
using ViabilityIQ.Shared.SharedModels;

namespace ViabilityIQ.Web.Components.Pages_Assessments.PageFormComponents
{
    public partial class DebtorsCreditorsFormComponent
    {
        [Parameter] public long AssessmentId { get; set; }
        [Parameter] public EventCallback<SaveResult> OnSaveComplete { get; set; }

        private async Task HandleSubmitData()
        {
            try
            {
                // Execute your SQL Stored Procedures or API services here
                // e.g., await ExpenseService.SaveAsync(AssessmentId, model);

                var result = new SaveResult
                {
                    Success = true,
                    ClosePanel = true, // true closes drawer, false keeps it open for more data entry
                    Message = "Configuration benchmarks updated successfully."
                };

                // Fire event back to main orchestrator page
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
