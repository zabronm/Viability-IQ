using Microsoft.AspNetCore.Components;
using ViabilityIQ.Application.Interfaces;
using ViabilityIQ.Shared.DataModels;
using ViabilityIQ.Shared.SharedModels;
using ViabilityIQ.Web.Services;

namespace ViabilityIQ.Web.Components.Pages.PageFormComponents
{
    public partial class NotificationItemDetailsComponent
    {
        [Inject] private INotificationItemRepository NotificationRepository { get; set; } = default!;
        [Inject] private OffCanvasStateService? OffcanvasService { get; set; } = default!;

        [Parameter] public NotificationItem NotificationItemData { get; set; } = new();
        [Parameter] public long CurrentUserId { get; set; } = 1;

        private async Task MarkCompletedAsync()
        {
            await NotificationRepository.MarkAsReadAsync(NotificationItemData.NotificationItemId);
            await NotificationRepository.UpdateTaskStatusAsync(NotificationItemData.NotificationItemId, (int)TaskStatusType.Completed);

            var saveResult = new SaveResult
            {
                Success = true,
                ClosePanel = true,
                Message = "Task marked as completed."
            };

            await OffcanvasService!.PublishResultAsync(saveResult);
        }

        private async Task CloseDrawerAsync()
        {
            var saveResult = new SaveResult
            {
                Success = false,
                ClosePanel = true,
                Message = string.Empty
            };
            await OffcanvasService!.PublishResultAsync(saveResult);
        }
    }
}
