using Microsoft.AspNetCore.Components;
using ViabilityIQ.Application.Interfaces;
using ViabilityIQ.Shared.DataModels;
using ViabilityIQ.Shared.SharedModels;
using ViabilityIQ.Web.Components.Pages.PageFormComponents;
using ViabilityIQ.Web.Services;

namespace ViabilityIQ.Web.Components.Pages
{
    public partial class NotificationsPage : IAsyncDisposable
    {
        [Inject] OffCanvasStateService? OffCanvasService { get; set; } = default;
        [Inject] private INotificationItemRepository NotificationRepository { get; set; } = default!;
        [Inject] ToastService? _Toast { get; set; }
        [Inject] ISessionService? sessionService { get; set; }

        private List<NotificationItem> notifications = new();
        private string activeTab = "all";
        private bool isLoading = true;
        private int unreadCount = 0;
        private long currentUserId = 1; // Replace with authenticated user claim parsing in production

        protected override async Task OnInitializedAsync()
        {
            OffCanvasService!.OnShow += HandleCanvasShow;
            await LoadDataAsync();
        }

        private async Task HandleCanvasShow(CanvasRequest request)
        {
            await Task.CompletedTask;
        }

        private async Task LoadDataAsync()
        {
            isLoading = true;
            try
            {
                notifications = await NotificationRepository.GetNotificationsAsync(currentUserId, activeTab);
                unreadCount = await NotificationRepository.GetUnreadCountAsync(currentUserId);
            }
            catch (Exception)
            {
                notifications = new();
                unreadCount = 0;
            }
            finally
            {
                isLoading = false;
                StateHasChanged();
            }
        }

        private async Task SetActiveTabAsync(string tab)
        {
            activeTab = tab;
            await LoadDataAsync();
        }

        private async Task OpenCreateDrawer()
        {
            await OffCanvasService!.ShowAsync(
                new CanvasRequest
                {
                    Title = "Compose Task / Message",
                    Width = 480,
                    ComponentType = typeof(NotificationItemFormComponent),
                    Parameters = new Dictionary<string, object>
                    {
                        { "CurrentUserId", currentUserId }
                    },
                    ResultCallback = async (result) => await ProcessExecutionFeedback(result)
                });
        }

        private async Task OpenDetails(NotificationItem item)
        {
            if (!item.IsRead)
            {
                await NotificationRepository.MarkAsReadAsync(item.NotificationItemId);
                item.IsRead = true;
                unreadCount = await NotificationRepository.GetUnreadCountAsync(currentUserId);
            }

            await OffCanvasService!.ShowAsync(
                new CanvasRequest
                {
                    Title = "Notification Details",
                    Width = 480,
                    ComponentType = typeof(NotificationItemDetailsComponent),
                    Parameters = new Dictionary<string, object>
                    {
                        { "NotificationItemData", item },
                        { "CurrentUserId", currentUserId }
                    },
                    ResultCallback = async (result) => await ProcessExecutionFeedback(result)
                });
        }

        private async Task ProcessExecutionFeedback(SaveResult _result)
        {
            if (_result.Success)
            {
                if (_Toast != null && sessionService != null)
                {
                    _Toast.ShowSuccess(_result.Message, sessionService.AppTitle);
                }
                await LoadDataAsync();
            }
            else if (!string.IsNullOrEmpty(_result.Message))
            {
                if (_Toast != null && sessionService != null)
                {
                    _Toast.ShowError(_result.Message, sessionService.AppTitle);
                }
            }
            StateHasChanged();
        }

        private async Task MarkAsReadAsync(int notificationId)
        {
            await NotificationRepository.MarkAsReadAsync(notificationId);
            await LoadDataAsync();
        }

        private async Task CompleteTaskAsync(int notificationId)
        {
            await NotificationRepository.MarkAsReadAsync(notificationId);
            await NotificationRepository.UpdateTaskStatusAsync(notificationId, (int)TaskStatusType.Completed);
            await LoadDataAsync();
        }

        private string GetActiveTabClass(string tab) => activeTab == tab ? "active fw-bold text-success" : "text-secondary";

        private string TruncateMessage(string message)
        {
            if (string.IsNullOrEmpty(message)) return string.Empty;
            return message.Length > 90 ? message.Substring(0, 90) + "..." : message;
        }

        private string GetAccentColor(NotificationItem item)
        {
            if (!item.IsRead) return "#0d6efd";
            if (item.IsTask)
            {
                return item.TaskStatus == TaskStatusType.Completed ? "#198754" : "#ffc107";
            }
            return "#6c757d";
        }

        public async ValueTask DisposeAsync()
        {
            if (OffCanvasService != null)
            {
                OffCanvasService.OnShow -= HandleCanvasShow;
            }
        }
    }
}