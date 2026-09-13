using Microsoft.AspNetCore.Components;
using ViabilityIQ.Application.Interfaces;
using ViabilityIQ.Shared.DataModels;
using ViabilityIQ.Shared.SharedModels;
using ViabilityIQ.Web.Services;


namespace ViabilityIQ.Web.Components.Pages.PageFormComponents
{
    public partial class NotificationItemFormComponent : ComponentBase
    {
        [Inject] private INotificationItemRepository notificationRepository { get; set; } = default!;
        [Inject] private OffCanvasStateService? OffcanvasService { get; set; } = default!;
        [Inject] private ISessionService? sessionService { get; set; }
        [Parameter] public long CurrentUserId { get; set; } = 1;

        private NotificationItem newItem = new();
        private bool isProcessing = false;

        protected override void OnInitialized()
        {
            newItem = new NotificationItem
            {
                SenderId = CurrentUserId,
                CreatedDate = DateTime.Now,
                TaskStatus = TaskStatusType.Pending,
                IsTask = false
            };
        }

        private async Task HandleSubmitAsync()
        {
            isProcessing = true;
            StateHasChanged();

            try
            {
                
                newItem.SenderId = CurrentUserId;
                newItem.Active = true;
                newItem.CreatedDate = DateTime.UtcNow;
                newItem.CreatedBy = sessionService!.UserId;
                await notificationRepository.InsertNotificationAsync(newItem);

                var saveResult = new SaveResult
                {
                    Success = true,
                    ClosePanel = true,
                    Message = "Communication dispatched successfully."
                };

                await OffcanvasService!.PublishResultAsync(saveResult);
            }
            catch (Exception ex)
            {
                var saveResult = new SaveResult
                {
                    Success = false,
                    ClosePanel = false,
                    Message = $"Error: {ex.Message}"
                };
                await OffcanvasService!.PublishResultAsync(saveResult);
            }
            finally
            {
                isProcessing = false;
                StateHasChanged();
            }
        }
    }
}