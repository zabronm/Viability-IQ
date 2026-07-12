using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using ViabilityIQ.Web.Services;

namespace ViabilityIQ.Web.Components.CommonComponents
{
    public partial class ZabOffCanvasHost: ComponentBase, IDisposable
    {
        private bool _isOpen = false;

        private string _title = "";

        private int _width = 350;

        private Type? _componentType;

        private Dictionary<string, object>? _parameters;

        protected override void OnInitialized()
        {
            CanvasService.OnShow += ShowCanvasAsync;
            CanvasService.OnClose += CloseCanvasAsync;
        }

        public void Dispose()
        {
            CanvasService.OnShow -= ShowCanvasAsync;
            CanvasService.OnClose -= CloseCanvasAsync;
        }

        private async Task ShowCanvasAsync(CanvasRequest request)
        {
            _title = request.Title;

            _width = request.Width;

            _componentType = request.ComponentType;

            _parameters = ConvertToDictionary(request.Parameters);

            _isOpen = true;

            StateHasChanged();

            await JS.InvokeVoidAsync(
                "zabOffCanvas.show",
                "zabCanvasHost");
        }


        private static Dictionary<string, object?> ConvertToDictionary(object? source)
        {
            var result = new Dictionary<string, object?>();

            if (source == null)
                return result;

            if (source is Dictionary<string, object?> dictionary)
                return dictionary;

            foreach (var property in source.GetType().GetProperties())
            {
                var value = property.GetValue(source);
                result[property.Name] = value;
            }

            return result;
        }


        private async Task CloseCanvasAsync()
        {
            await JS.InvokeVoidAsync(
                "zabOffCanvas.hide",
                "zabCanvasHost");

            _isOpen = false;

            _componentType = null;

            _parameters = null;

            StateHasChanged();
        }




    }
}

