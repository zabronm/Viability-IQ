using Microsoft.AspNetCore.Components;
using ViabilityIQ.Web.Components.CommonComponents;
using ViabilityIQ.Web.Services;

namespace ViabilityIQ.Web.Components.Layout
{
    public partial class MainLayout
    {
        [Inject] private OffCanvasStateService OffCanvasService { get; set; } = default!;
        private bool isSidebarCollapsed = false;
        private void ToggleSidebar() => isSidebarCollapsed = !isSidebarCollapsed;

        private ZabOffCanvas? OffCanvasControlRef;
        private string currentTitle = string.Empty;
        private int currentWidth = 550;
        private Type? dynamicComponentType;
        private Dictionary<string, object?> dynamicParameters = new();
        private bool isCanvasOpen = false;

        protected override void OnInitialized()
        {
            OffCanvasService.OnShow += async (request) =>
            {
                currentTitle = request.Title;

                if (request.Width != null)
                {
                    var widthStr = request.Width.ToString();
                    if (!string.IsNullOrEmpty(widthStr) && int.TryParse(widthStr.Replace("px", "").Trim(), out var parsedWidth))
                    {
                        currentWidth = parsedWidth;
                    }
                }

                dynamicComponentType = request.ComponentType;
                dynamicParameters = OffCanvasStateService.ConvertToDictionary(request.Parameters);

                isCanvasOpen = true; // Force open via state variable
                StateHasChanged();

                await Task.CompletedTask;
            };

            OffCanvasService.OnClose += async () =>
            {
                isCanvasOpen = false; // Force close via state variable
                StateHasChanged();

                await Task.Delay(300); // Wait for close animation
                dynamicComponentType = null;
                dynamicParameters.Clear();
                StateHasChanged();
            };
        }
    }
}