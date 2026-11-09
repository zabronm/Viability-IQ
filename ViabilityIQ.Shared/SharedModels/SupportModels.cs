using System.ComponentModel.DataAnnotations;

namespace ViabilityIQ.Shared.SharedModels;

public sealed class SupportConversation
{
    public long SupportConversationId { get; set; }
    public long UserId { get; set; }
    public long TenantId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string UserEmail { get; set; } = string.Empty;
    public long? ConsultantUserId { get; set; }
    public string? ConsultantName { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime LastUserActivityAtUtc { get; set; }
    public DateTime? ClosedAtUtc { get; set; }
    public string? LastMessage { get; set; }
    public DateTime? LastMessageAtUtc { get; set; }
}

public sealed class SupportMessage
{
    public long SupportMessageId { get; set; }
    public long SupportConversationId { get; set; }
    public long SenderUserId { get; set; }
    public string SenderName { get; set; } = string.Empty;
    public bool IsConsultant { get; set; }
    public string MessageText { get; set; } = string.Empty;
    public DateTime SentAtUtc { get; set; }
}

public sealed class SupportTicketRequest
{
    [Required, StringLength(160, MinimumLength = 4)]
    public string Title { get; set; } = string.Empty;

    [Required, StringLength(10000, MinimumLength = 10)]
    public string Message { get; set; } = string.Empty;
}

public sealed record SupportFileAttachment(
    string FileName,
    string ContentType,
    byte[] Content);

public sealed record SupportTicketResult(
    long TicketId,
    string Reference,
    bool EmailDelivered,
    string UserMessage);
