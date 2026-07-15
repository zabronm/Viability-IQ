using ViabilityIQ.Web.Components.CommonComponents;

namespace ViabilityIQ.Web.Components.Pages
{
    public partial class Settings
    {
        //---------------------------------------------------------
        // Alert
        //---------------------------------------------------------
        private bool blAlert = true;
        private ViqAlertComponent.AlertSeverity AlertSeverity = ViqAlertComponent.AlertSeverity.Success;
        private string AlertHeading = "Global Settings";
        private string AlertMessage = "Setup all base GLOBAL master data that will be assumed and/or inherited by the assessments.";

    }
}
