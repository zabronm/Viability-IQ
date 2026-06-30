using OfficeOpenXml.FormulaParsing.Excel.Functions;

namespace ViabilityIQ.Web.Services
{
    public sealed class ZabOffCanvasService
    {
        public event Func<CanvasRequest, Task>? OnShow;
        public event Func<Task>? OnClose;
        public async Task ShowAsync(CanvasRequest request)
        {
            if (OnShow != null)
            {
                await OnShow.Invoke(request);
            }
        }

        public async Task CloseAsync()
        {
            if (OnClose != null)
            {
                await OnClose.Invoke();
            }

        }
    }
}
