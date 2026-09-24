using ViabilityIQ.Shared.DataModels;

namespace ViabilityIQ.Web.Components.Pages_Website
{
    public partial class Website
    {
        private LeadSubmission _leadModel = new();
        private bool _isSubmitting = false;
        private bool _isSuccess = false;
        private string _statusMessage = string.Empty;

        private async Task HandleSubmit()
        {
            _isSubmitting = true;
            _statusMessage = string.Empty;

            try
            {
                await LeadRepository.InsertLeadAsync(_leadModel);

                _isSuccess = true;
                _statusMessage = "Thank you! Your demo request has been submitted successfully.";
                _leadModel = new();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error saving lead submission.");
                _isSuccess = false;
                _statusMessage = "An error occurred while saving your request. Please try again.";
            }
            finally
            {
                _isSubmitting = false;
            }
        }
    }
}
