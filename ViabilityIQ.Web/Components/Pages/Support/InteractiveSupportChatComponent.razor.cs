using Microsoft.AspNetCore.Components;
using ViabilityIQ.Application.Interfaces;
using ViabilityIQ.Shared.SharedModels;

namespace ViabilityIQ.Web.Components.Pages.Support;

public partial class InteractiveSupportChatComponent
{
    [Inject] private ISupportService SupportService { get; set; } = default!;
    [Inject] private ILogger<InteractiveSupportChatComponent> Logger { get; set; } = default!;

    private SupportConversation? _conversation;
    private List<SupportMessage> _messages = [];
    private SupportMessageDraft _draft = new();
    private CancellationTokenSource? _pollCancellation;
    private Task? _pollTask;
    private string? _error;
    private bool _isLoading = true;
    private bool _isSending;

    protected override async Task OnInitializedAsync()
    {
        try
        {
            _conversation = await SupportService.StartConversationAsync();
            await RefreshMessagesAsync();
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "Unable to initialize user support chat.");
            _error = "Support chat is temporarily unavailable. Please try again.";
        }
        finally
        {
            _isLoading = false;
        }

        _pollCancellation = new CancellationTokenSource();
        _pollTask = PollConversationAsync(_pollCancellation.Token);
    }

    private async Task StartConversationAsync()
    {
        _error = null;
        try
        {
            _conversation = await SupportService.StartConversationAsync();
            _draft = new();
            await RefreshMessagesAsync();
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "Unable to start a user support conversation.");
            _error = "We could not start a support chat. Please try again.";
        }
    }

    private async Task SendMessageAsync()
    {
        if (_conversation is null || _isSending)
            return;

        _isSending = true;
        _error = null;
        try
        {
            await SupportService.SendUserMessageAsync(_conversation.SupportConversationId, _draft.Message);
            _draft = new();
            await RefreshMessagesAsync();
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "Unable to send a user support message.");
            _error = exception is InvalidOperationException
                ? exception.Message
                : "Your message could not be sent. Please try again.";
        }
        finally
        {
            _isSending = false;
        }
    }

    private async Task CloseConversationAsync()
    {
        if (_conversation is null)
            return;

        try
        {
            await SupportService.CloseConversationAsync(_conversation.SupportConversationId);
            await RefreshMessagesAsync();
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "Unable to close user support conversation {ConversationId}.", _conversation.SupportConversationId);
            _error = "The conversation could not be closed. Please try again.";
        }
    }

    private async Task PollConversationAsync(CancellationToken cancellationToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(4));
        try
        {
            while (await timer.WaitForNextTickAsync(cancellationToken))
            {
                if (_conversation is null)
                    continue;

                try
                {
                    await RefreshMessagesAsync(cancellationToken);
                    await InvokeAsync(StateHasChanged);
                }
                catch (Exception exception)
                {
                    Logger.LogError(exception, "Unable to refresh user support conversation {ConversationId}.", _conversation.SupportConversationId);
                    _error = "We could not refresh the conversation. Reconnecting...";
                    await InvokeAsync(StateHasChanged);
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
    }

    private async Task RefreshMessagesAsync(CancellationToken cancellationToken = default)
    {
        if (_conversation is null)
            return;

        _conversation = await SupportService.GetConversationAsync(
            _conversation.SupportConversationId,
            cancellationToken);
        _messages = (await SupportService.GetMessagesAsync(
            _conversation.SupportConversationId,
            cancellationToken)).ToList();
    }

    public async ValueTask DisposeAsync()
    {
        if (_pollCancellation is null)
            return;

        await _pollCancellation.CancelAsync();
        if (_pollTask is not null)
        {
            try
            {
                await _pollTask;
            }
            catch (OperationCanceledException)
            {
            }
        }

        _pollCancellation.Dispose();
    }

    private sealed class SupportMessageDraft
    {
        [System.ComponentModel.DataAnnotations.Required]
        [System.ComponentModel.DataAnnotations.StringLength(4000, MinimumLength = 1)]
        public string Message { get; set; } = string.Empty;
    }
}
