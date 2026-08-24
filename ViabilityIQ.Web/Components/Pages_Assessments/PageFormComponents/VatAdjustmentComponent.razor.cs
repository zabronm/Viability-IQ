using Microsoft.AspNetCore.Components;

namespace ViabilityIQ.Web.Components.Pages_Assessments.PageFormComponents
{
    public partial class VatAdjustmentComponent: ComponentBase
    {
        [Parameter] public EventCallback OnSave { get; set; }

        private bool _isOpen = false;
        private decimal[] outputAdjustments = new decimal[12];
        private decimal[] inputAdjustments = new decimal[12];
        private string[] monthNotes = new string[12];

        public void Show() => _isOpen = true;
        public void Close() => _isOpen = false;

        private async Task Save()
        {
            Close();
            if (OnSave.HasDelegate)
                await OnSave.InvokeAsync();
        }
    }
}
