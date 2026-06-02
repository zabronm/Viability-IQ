using ViabilityIQ.Shared.SharedModels;

namespace ViabilityIQ.Web.Services
{
    public class ToastService
    {
        public event Action<ToastMessage>? OnShow;

        public void ShowSuccess(string message, string title = "Success")
                => ShowToast(title, message, "toast-success", "bi-check-circle-fill");

        public void ShowError(string message, string title = "Error")
                => ShowToast(title, message, "toast-danger", "bi-x-octagon-fill");

        public void ShowWarning(string message, string title = "Warning")
                => ShowToast(title, message, "toast-warning", "bi-exclamation-triangle-fill");

        public void ShowInfo(string message, string title = "Information")
                => ShowToast(title, message, "toast-info", "bi-info-circle-fill");


        private void ShowToast(
            string title,
            string message,
            string cssClass,
            string icon)
        {
            OnShow?.Invoke(new ToastMessage
            {
                Title = title,
                Message = message,
                CssClass = cssClass,
                Icon = icon,
                Duration = 4000
            });
        }
    }
}
