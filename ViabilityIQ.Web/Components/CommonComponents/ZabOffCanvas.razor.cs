using Microsoft.AspNetCore.Components;

namespace ViabilityIQ.Web.Components.CommonComponents
{
    public partial class ZabOffCanvas
    {
        [Parameter] public bool IsOpen { get; set; } = false;
        [Parameter] public int dWidth { get; set; } = 458;
        [Parameter] public EventCallback<bool> IsOpenChanged { get; set; }
        [Parameter] public string HeaderTitle { get; set; } = "Workspace Form Operations";
        [Parameter] public RenderFragment? ChildContent { get; set; }

        public async Task OpenAsync(string customTitle = "")
        {
            if (!string.IsNullOrWhiteSpace(customTitle))
            {
                HeaderTitle = customTitle;
            }
            IsOpen = true;
            await IsOpenChanged.InvokeAsync(IsOpen);
            StateHasChanged();
        }

        public async Task CloseAsync()
        {
            IsOpen = false;
            await IsOpenChanged.InvokeAsync(IsOpen);
            StateHasChanged();
        }
    }
}
