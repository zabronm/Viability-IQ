using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using ViabilityIQ.Shared.SharedModels;

namespace ViabilityIQ.Web.Services
{
    public sealed class ZabOffCanvasService
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

            // Remember who opened this editor
            _currentResultCallback = request.ResultCallback;

            if (OnShow != null)
            {
                await OnShow.Invoke(request);
            }
        }

        //---------------------------------------------------------
        // Hide / Dismiss OffCanvas (Bridges directly into Publish pipeline)
        //---------------------------------------------------------
        public async Task HideAsync(SaveResult result)
        {
            // Forwards execution to process callbacks and manage UI lifecycle panels cleanly
            await PublishResultAsync(result);
        }

        //---------------------------------------------------------
        // Publish SaveResult back to the calling page
        //---------------------------------------------------------

        public async Task PublishResultAsync(SaveResult result)
        {
            // Notify the page that opened this editor
            if (_currentResultCallback != null)
            {
                await _currentResultCallback.Invoke(result);
            }

            // Should the OffCanvas close?
            if (result.ClosePanel)
            {
                // Forget the callback
                _currentResultCallback = null;

                // Close the OffCanvas
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