using Microsoft.AspNetCore.Components;
using ViabilityIQ.Application.Interfaces;
using ViabilityIQ.Shared.SharedModels;
using ViabilityIQ.Web.Components.Layout;

namespace ViabilityIQ.Web.Components.Pages.Support;

public partial class SupportConsultantInbox
{
    [Inject] private ISupportService SupportService { get; set; } = default!;
    [Inject] private ILogger<SupportConsultantInbox> Logger { get; set; } = default!;

    private List<SupportConversation> _conversations = [];
    private List<SupportMessage> _messages = [];
    private SupportConversation? _selected;
    private ConsultantReplyDraft _draft = new();
    private CancellationTokenSource? _pollCancellation;
    private Task? _pollTask;
    private string? _error;
    private bool _isLoading = true;
    private bool _isSending;

    protected override async Task OnInitializedAsync()
    {
        var canStartPolling = true;
        try
        {
            await RefreshInboxAsync();
        }
        catch (UnauthorizedAccessException exception)
        {
            Logger.LogWarning(exception, "Support chat inbox opened without a database Support Consultant role.");
            _error = "Your account does not currently have Support Consultant access. Ask an administrator to grant access, then sign out and back in.";
            canStartPolling = false;
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "Unable to initialize the support consultant inbox.");
            _error = "The support inbox could not be loaded. Please try again.";
        }
        finally
        {
            _isLoading = false;
        }

        if (canStartPolling)
        {
            _pollCancellation = new CancellationTokenSource();
            _pollTask = PollInboxAsync(_pollCancellation.Token);
        }
    }

    private async Task RefreshInboxAsync(CancellationToken cancellationToken = default)
    {
        _conversations = (await SupportService.GetConsultantConversationsAsync(cancellationToken)).ToList();
        if (_selected is null)
            return;

        _selected = _conversations.FirstOrDefault(
            item => item.SupportConversationId == _selected.SupportConversationId);
        if (_selected is not null)
            _messages = (await SupportService.GetMessagesAsync(_selected.SupportConversationId, cancellationToken)).ToList();
    }

    private async Task SelectConversationAsync(SupportConversation conversation)
    {
        _selected = conversation;
        _messages = (await SupportService.GetMessagesAsync(conversation.SupportConversationId)).ToList();
        _error = null;
    }

    private async Task ClaimSelectedAsync()
    {
        if (_selected is null || _isSending)
            return;

        _isSending = true;
        try
        {
            _selected = await SupportService.ClaimConversationAsync(_selected.SupportConversationId);
            await RefreshInboxAsync();
            if (_selected is not null)
                _messages = (await SupportService.GetMessagesAsync(_selected.SupportConversationId)).ToList();
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "Unable to claim support conversation.");
            _error = exception.Message;
        }
        finally
        {
            _isSending = false;
        }
    }

    private async Task SendReplyAsync()
    {
        if (_selected is null || _isSending)
            return;

        _isSending = true;
        try
        {
            await SupportService.SendConsultantMessageAsync(_selected.SupportConversationId, _draft.Message);
            _draft = new();
            _messages = (await SupportService.GetMessagesAsync(_selected.SupportConversationId)).ToList();
            await RefreshInboxAsync();
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "Unable to send consultant reply for conversation {ConversationId}.", _selected.SupportConversationId);
            _error = exception.Message;
        }
        finally
        {
            _isSending = false;
        }
    }

    private async Task CloseSelectedAsync()
    {
        if (_selected is null)
            return;
        try
        {
            await SupportService.CloseConversationAsync(_selected.SupportConversationId);
            _selected = null;
            _messages.Clear();
            await RefreshInboxAsync();
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "Unable to close support conversation.");
            _error = exception.Message;
        }
    }

    private async Task PollInboxAsync(CancellationToken cancellationToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(5));
        try
        {
            while (await timer.WaitForNextTickAsync(cancellationToken))
            {
                try
                {
                    await RefreshInboxAsync(cancellationToken);
                    await InvokeAsync(StateHasChanged);
                }
                catch (UnauthorizedAccessException exception)
                {
                    Logger.LogWarning(exception, "Support consultant access was revoked while the inbox was open.");
                    _error = "Your Support Consultant access is no longer available. Ask an administrator to review your access.";
                    await InvokeAsync(StateHasChanged);
                    return;
                }
                catch (Exception exception)
                {
                    Logger.LogError(exception, "Unable to refresh the support consultant inbox.");
                    _error = "The inbox could not be refreshed. Reconnecting...";
                    await InvokeAsync(StateHasChanged);
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
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

    private sealed class ConsultantReplyDraft
    {
        [System.ComponentModel.DataAnnotations.Required]
        [System.ComponentModel.DataAnnotations.StringLength(4000, MinimumLength = 1)]
        public string Message { get; set; } = string.Empty;
    }
}
