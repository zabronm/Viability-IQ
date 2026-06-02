using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace BrucolWeb.Web.Components.Common
{
    public partial class DocumentPreviewComponent
    {
        [Inject]        public IJSRuntime JS        { get; set; } = default!;

        [Parameter]        public string? FileUrl        { get; set; }

        private bool IsPdf =>            FileUrl?.EndsWith(".pdf") == true;

        private bool IsImage =>
            FileUrl?.EndsWith(".png") == true
            ||
            FileUrl?.EndsWith(".jpg") == true
            ||
            FileUrl?.EndsWith(".jpeg") == true;

        private async Task PrintDocument()
        {
            await JS.InvokeVoidAsync(                "window.open",                FileUrl,                "_blank");
        }

    }
}
