using System.Reflection;
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
        // Show OffCanvas
        //---------------------------------------------------------

        public async Task ShowAsync(CanvasRequest request)
        {
            CurrentRequest = request;

            if (OnShow != null)
                await OnShow.Invoke(request);
        }

        //---------------------------------------------------------
        // Close OffCanvas
        //---------------------------------------------------------

        public async Task CloseAsync()
        {
            if (OnClose != null)
                await OnClose.Invoke();
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