using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;


namespace BrucolWeb.Web.Components.Common
{
    public partial class ZabOffCanvas : IAsyncDisposable
    {
        [Inject] IJSRuntime JS { get; set; }
        [Parameter] public string Id { get; set; } = "zabOffcanvas";
        [Parameter] public string Title { get; set; } = "Panel";
        [Parameter] public RenderFragment? ChildContent { get; set; }  // 🔥 This is the key
        [Parameter] public string? dWidth { get; set; } = "400px;";
        [Parameter] public RenderFragment? FooterContent { get; set; }

        public async Task Show()
        {
            await Task.Delay(50);
            await JS.InvokeVoidAsync("ui.showOffcanvas", Id);
        }

        public async Task Close()
        {
            await JS.InvokeVoidAsync("ui.hideOffcanvas", Id);
        }

        public async ValueTask DisposeAsync()
        {
            try
            {
                await JS.InvokeVoidAsync("ui.hideOffcanvas", Id);
            }
            catch
            {
            }
        }

    }
}

