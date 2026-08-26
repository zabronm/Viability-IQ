using System.Reflection;
using ViabilityIQ.Shared.SharedModels;

namespace ViabilityIQ.Web.Services
{
    public class OffCanvasStateService
    {
        //---------------------------------------------------------
        // Events
        //---------------------------------------------------------
        public event Func<CanvasRequest, Task>? OnShow;
        public event Func<Task>? OnClose;

        //---------------------------------------------------------
        // Current Request
        //---------------------------------------------------------
        public CanvasRequest? CurrentRequest { get; private set; }

        //---------------------------------------------------------
        // Callback to the page that opened the editor
        //---------------------------------------------------------
        private Func<SaveResult, Task>? _currentResultCallback;

        //---------------------------------------------------------
        // Show OffCanvas
        //---------------------------------------------------------
        public async Task ShowAsync(CanvasRequest request)
        {
            CurrentRequest = request;
            _currentResultCallback = request.ResultCallback;

            if (OnShow != null)
            {
                await OnShow.Invoke(request);
            }
        }

        //---------------------------------------------------------
        // Publish SaveResult back to the calling page
        //---------------------------------------------------------
        public async Task PublishResultAsync(SaveResult result)
        {
            // ✅ Notify the page that opened this editor
            if (_currentResultCallback != null)
            {
                await _currentResultCallback.Invoke(result);
            }

            // Should the OffCanvas close?
            if (result.ClosePanel)
            {
                _currentResultCallback = null;
                await CloseAsync();
            }
        }

        //---------------------------------------------------------
        // Close OffCanvas
        //---------------------------------------------------------
        public async Task CloseAsync()
        {
            if (OnClose != null)
            {
                await OnClose.Invoke();
            }

            CurrentRequest = null;
        }

        //---------------------------------------------------------
        // Helper
        //---------------------------------------------------------
        public static Dictionary<string, object?> ConvertToDictionary(object? source)
        {
            var result = new Dictionary<string, object?>();

            if (source == null)
                return result;

            if (source is Dictionary<string, object?> dictionary)
                return dictionary;

            foreach (PropertyInfo property in source.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                result[property.Name] = property.GetValue(source);
            }

            return result;
        }
    }
}