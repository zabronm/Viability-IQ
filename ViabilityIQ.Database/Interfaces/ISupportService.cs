using ViabilityIQ.Shared.SharedModels;

namespace ViabilityIQ.Application.Interfaces;

public interface ISupportService
{
    Task<SupportConversation> StartConversationAsync(CancellationToken cancellationToken = default);

    Task<SupportConversation> GetConversationAsync(
        long conversationId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SupportMessage>> GetMessagesAsync(
        long conversationId,
        CancellationToken cancellationToken = default);

    Task<SupportMessage> SendUserMessageAsync(
        long conversationId,
        string message,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SupportConversation>> GetWaitingConversationsAsync(
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SupportConversation>> GetConsultantConversationsAsync(
        CancellationToken cancellationToken = default);

    Task<SupportConversation> ClaimConversationAsync(
        long conversationId,
        CancellationToken cancellationToken = default);

    Task<SupportMessage> SendConsultantMessageAsync(
        long conversationId,
        string message,
        CancellationToken cancellationToken = default);

    Task CloseConversationAsync(
        long conversationId,
        CancellationToken cancellationToken = default);

    Task ExpireIdleConversationsAsync(CancellationToken cancellationToken = default);

    Task<SupportTicketResult> SubmitTicketAsync(
        SupportTicketRequest request,
        IReadOnlyList<SupportFileAttachment> attachments,
        CancellationToken cancellationToken = default);
}
