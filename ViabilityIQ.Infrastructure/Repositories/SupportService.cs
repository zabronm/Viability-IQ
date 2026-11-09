using System.Data;
using System.Net;
using Dapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ViabilityIQ.Application.Interfaces;
using ViabilityIQ.Infrastructure.DbFactory;
using ViabilityIQ.Shared.SharedModels;

namespace ViabilityIQ.Infrastructure.Repositories;

public sealed class SupportService(
    IDbConnectionFactory connectionFactory,
    ISessionService sessionService,
    IEmailReportingService emailReportingService,
    IConfiguration configuration,
    ILogger<SupportService> logger) : ISupportService
{
    private const int IdleTimeoutMinutes = 10;
    private const int MaximumAttachments = 5;
    private const int MaximumAttachmentBytes = 10 * 1024 * 1024;
    private const int MaximumTotalAttachmentBytes = 25 * 1024 * 1024;

    public async Task<SupportConversation> StartConversationAsync(
        CancellationToken cancellationToken = default)
    {
        EnsureSignedIn();
        await ExpireIdleConversationsAsync(cancellationToken);

        using var connection = connectionFactory.CreateConnection();
        var existing = await connection.QuerySingleOrDefaultAsync<SupportConversation>(
            new CommandDefinition(
                ConversationSelect + """
                    WHERE c.UserId = @UserId
                      AND c.TenantId = @TenantId
                      AND c.Status IN (N'Waiting', N'Active');
                    """,
                new { UserId = sessionService.UserId, TenantId = sessionService.TenantId },
                cancellationToken: cancellationToken));
        if (existing is not null)
            return existing;

        var now = DateTime.UtcNow;
        var conversationId = await connection.ExecuteScalarAsync<long>(
            new CommandDefinition(
                """
                INSERT INTO dbo.SupportConversations
                    (UserId, TenantId, UserName, UserEmail, Status,
                     CreatedAtUtc, LastUserActivityAtUtc)
                OUTPUT INSERTED.SupportConversationId
                VALUES
                    (@UserId, @TenantId, @UserName, @UserEmail, N'Waiting',
                     @CreatedAtUtc, @LastUserActivityAtUtc);
                """,
                new
                {
                    sessionService.UserId,
                    sessionService.TenantId,
                    UserName = sessionService.UserName,
                    UserEmail = sessionService.UserEmail,
                    CreatedAtUtc = now,
                    LastUserActivityAtUtc = now
                },
                cancellationToken: cancellationToken));

        return await GetConversationAsync(conversationId, cancellationToken);
    }

    public async Task<SupportConversation> GetConversationAsync(
        long conversationId,
        CancellationToken cancellationToken = default)
    {
        await ExpireIdleConversationsAsync(cancellationToken);
        using var connection = connectionFactory.CreateConnection();
        await EnsureConversationAccessAsync(connection, conversationId, cancellationToken);

        var conversation = await connection.QuerySingleOrDefaultAsync<SupportConversation>(
            new CommandDefinition(
                ConversationSelect + "WHERE c.SupportConversationId = @ConversationId;",
                new { ConversationId = conversationId },
                cancellationToken: cancellationToken));

        return conversation
            ?? throw new InvalidOperationException("The support conversation is no longer available.");
    }

    public async Task<IReadOnlyList<SupportMessage>> GetMessagesAsync(
        long conversationId,
        CancellationToken cancellationToken = default)
    {
        using var connection = connectionFactory.CreateConnection();
        await EnsureConversationAccessAsync(connection, conversationId, cancellationToken);
        await connection.ExecuteAsync(
            new CommandDefinition(
                """
                UPDATE dbo.tblNotificationItem
                SET IsRead = 1
                WHERE SupportConversationId = @ConversationId
                  AND IsSupportMessage = 1
                  AND RecipientId = @UserId
                  AND IsRead = 0;
                """,
                new { ConversationId = conversationId, UserId = sessionService.UserId },
                cancellationToken: cancellationToken));

        var messages = await connection.QueryAsync<SupportMessage>(
            new CommandDefinition(
                """
                SELECT n.NotificationItemId AS SupportMessageId,
                       n.SupportConversationId,
                       n.SenderId AS SenderUserId,
                       n.SupportSenderName AS SenderName,
                       CAST(CASE WHEN n.SenderId <> c.UserId THEN 1 ELSE 0 END AS BIT) AS IsConsultant,
                       n.Message AS MessageText,
                       n.CreatedDate AS SentAtUtc
                FROM dbo.tblNotificationItem n
                INNER JOIN dbo.SupportConversations c
                    ON c.SupportConversationId = n.SupportConversationId
                WHERE n.SupportConversationId = @ConversationId
                  AND n.IsSupportMessage = 1
                ORDER BY n.CreatedDate, n.NotificationItemId;
                """,
                new { ConversationId = conversationId },
                cancellationToken: cancellationToken));
        return messages.AsList();
    }

    public async Task<SupportMessage> SendUserMessageAsync(
        long conversationId,
        string message,
        CancellationToken cancellationToken = default)
    {
        EnsureSignedIn();
        var text = ValidateMessage(message);
        await ExpireIdleConversationsAsync(cancellationToken);
        using var connection = connectionFactory.CreateConnection();
        await EnsureUserOwnsConversationAsync(connection, conversationId, cancellationToken);
        var now = DateTime.UtcNow;
        return await InsertMessageAsync(
            connection,
            conversationId,
            sessionService.UserId,
            sessionService.UserName,
            false,
            text,
            now,
            cancellationToken,
            """
            UPDATE dbo.SupportConversations
            SET LastUserActivityAtUtc = @SentAtUtc
            WHERE SupportConversationId = @ConversationId
              AND UserId = @SenderUserId
              AND Status IN (N'Waiting', N'Active');
            """);
    }

    public async Task<IReadOnlyList<SupportConversation>> GetWaitingConversationsAsync(
        CancellationToken cancellationToken = default)
    {
        await ExpireIdleConversationsAsync(cancellationToken);
        using var connection = connectionFactory.CreateConnection();
        await EnsureConsultantAsync(connection, cancellationToken);
        var conversations = await connection.QueryAsync<SupportConversation>(
            new CommandDefinition(
                ConversationSelect + """
                    WHERE c.Status = N'Waiting'
                    ORDER BY c.CreatedAtUtc, c.SupportConversationId;
                    """,
                cancellationToken: cancellationToken));
        return conversations.AsList();
    }

    public async Task<IReadOnlyList<SupportConversation>> GetConsultantConversationsAsync(
        CancellationToken cancellationToken = default)
    {
        await ExpireIdleConversationsAsync(cancellationToken);
        using var connection = connectionFactory.CreateConnection();
        await EnsureConsultantAsync(connection, cancellationToken);
        var conversations = await connection.QueryAsync<SupportConversation>(
            new CommandDefinition(
                ConversationSelect + """
                    WHERE c.Status = N'Waiting'
                       OR (c.Status = N'Active' AND c.ConsultantUserId = @ConsultantUserId)
                    ORDER BY CASE WHEN c.Status = N'Active' THEN 0 ELSE 1 END,
                             COALESCE(c.LastMessageAtUtc, c.CreatedAtUtc) DESC;
                    """,
                new { ConsultantUserId = sessionService.UserId },
                cancellationToken: cancellationToken));
        return conversations.AsList();
    }

    public async Task<SupportConversation> ClaimConversationAsync(
        long conversationId,
        CancellationToken cancellationToken = default)
    {
        await ExpireIdleConversationsAsync(cancellationToken);
        using var connection = connectionFactory.CreateConnection();
        if (connection.State != ConnectionState.Open)
            connection.Open();
        using var transaction = connection.BeginTransaction();
        try
        {
            await EnsureConsultantAsync(connection, cancellationToken, transaction);
            var conversation = await connection.QuerySingleOrDefaultAsync<SupportConversation>(
                new CommandDefinition(
                    ConversationSelect + """
                        WHERE c.SupportConversationId = @ConversationId;
                        """,
                    new { ConversationId = conversationId },
                    transaction,
                    cancellationToken: cancellationToken));

            if (conversation is null || conversation.Status != "Waiting")
                throw new InvalidOperationException("This support conversation has already been claimed or closed.");

            var claimed = await connection.ExecuteAsync(
                new CommandDefinition(
                    """
                    UPDATE dbo.SupportConversations
                    SET ConsultantUserId = @ConsultantUserId,
                        ConsultantName = @ConsultantName,
                        Status = N'Active'
                    WHERE SupportConversationId = @ConversationId
                      AND Status = N'Waiting';
                    """,
                    new
                    {
                        ConsultantUserId = sessionService.UserId,
                        ConsultantName = sessionService.UserName,
                        ConversationId = conversationId
                    },
                    transaction,
                    cancellationToken: cancellationToken));
            if (claimed != 1)
                throw new InvalidOperationException("This support conversation was claimed by another consultant.");

            await connection.ExecuteAsync(
                new CommandDefinition(
                    """
                    UPDATE dbo.tblNotificationItem
                    SET RecipientId = @ConsultantUserId,
                        IsRead = 1
                    WHERE SupportConversationId = @ConversationId
                      AND IsSupportMessage = 1
                      AND SenderId = @UserId;
                    """,
                    new
                    {
                        ConsultantUserId = sessionService.UserId,
                        ConversationId = conversationId,
                        UserId = conversation.UserId
                    },
                    transaction,
                    cancellationToken: cancellationToken));

            var firstName = conversation.UserName.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault()
                ?? "there";
            var greeting = $"Hi {firstName}, thank you for using our platform. My name is {sessionService.UserName}, how can I assist you today?";
            await InsertMessageAsync(
                connection,
                conversationId,
                sessionService.UserId,
                sessionService.UserName,
                true,
                greeting,
                DateTime.UtcNow,
                cancellationToken,
                null,
                transaction);

            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }

        return await GetConversationAsync(conversationId, cancellationToken);
    }

    public async Task<SupportMessage> SendConsultantMessageAsync(
        long conversationId,
        string message,
        CancellationToken cancellationToken = default)
    {
        var text = ValidateMessage(message);
        using var connection = connectionFactory.CreateConnection();
        await EnsureConsultantAsync(connection, cancellationToken);
        var assigned = await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(
                """
                SELECT COUNT(1)
                FROM dbo.SupportConversations
                WHERE SupportConversationId = @ConversationId
                  AND ConsultantUserId = @ConsultantUserId
                  AND Status = N'Active';
                """,
                new
                {
                    ConversationId = conversationId,
                    ConsultantUserId = sessionService.UserId
                },
                cancellationToken: cancellationToken));
        if (assigned == 0)
            throw new UnauthorizedAccessException("Only the assigned consultant can reply to this conversation.");

        return await InsertMessageAsync(
            connection,
            conversationId,
            sessionService.UserId,
            sessionService.UserName,
            true,
            text,
            DateTime.UtcNow,
            cancellationToken,
            """
            UPDATE dbo.SupportConversations
            SET ConsultantUserId = @SenderUserId
            WHERE SupportConversationId = @ConversationId
              AND ConsultantUserId = @SenderUserId
              AND Status = N'Active';
            """);
    }

    public async Task CloseConversationAsync(
        long conversationId,
        CancellationToken cancellationToken = default)
    {
        using var connection = connectionFactory.CreateConnection();
        var isConsultant = await IsConsultantAsync(connection, cancellationToken);
        var changed = await connection.ExecuteAsync(
            new CommandDefinition(
                """
                UPDATE dbo.SupportConversations
                SET Status = N'Closed', ClosedAtUtc = @ClosedAtUtc
                WHERE SupportConversationId = @ConversationId
                  AND Status IN (N'Waiting', N'Active')
                  AND ((UserId = @UserId AND TenantId = @TenantId)
                       OR (@IsConsultant = 1 AND ConsultantUserId = @UserId));
                """,
                new
                {
                    ConversationId = conversationId,
                    UserId = sessionService.UserId,
                    TenantId = sessionService.TenantId,
                    IsConsultant = isConsultant ? 1 : 0,
                    ClosedAtUtc = DateTime.UtcNow
                },
                cancellationToken: cancellationToken));
        if (changed != 1)
            throw new UnauthorizedAccessException("You cannot close this support conversation.");
    }

    public async Task ExpireIdleConversationsAsync(CancellationToken cancellationToken = default)
    {
        using var connection = connectionFactory.CreateConnection();
        await connection.ExecuteAsync(
            new CommandDefinition(
                """
                UPDATE dbo.SupportConversations
                SET Status = N'Expired', ClosedAtUtc = @NowUtc
                WHERE Status IN (N'Waiting', N'Active')
                  AND LastUserActivityAtUtc <= @IdleCutoffUtc;
                """,
                new
                {
                    NowUtc = DateTime.UtcNow,
                    IdleCutoffUtc = DateTime.UtcNow.AddMinutes(-IdleTimeoutMinutes)
                },
                cancellationToken: cancellationToken));
    }

    public async Task<SupportTicketResult> SubmitTicketAsync(
        SupportTicketRequest request,
        IReadOnlyList<SupportFileAttachment> attachments,
        CancellationToken cancellationToken = default)
    {
        EnsureSignedIn();
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(attachments);
        ValidateTicket(request, attachments);

        using var connection = connectionFactory.CreateConnection();
        var ticketId = await connection.ExecuteScalarAsync<long>(
            new CommandDefinition(
                """
                INSERT INTO dbo.SupportTickets
                    (UserId, TenantId, UserName, UserEmail, Title, Message,
                     Status, CreatedAtUtc)
                OUTPUT INSERTED.SupportTicketId
                VALUES
                    (@UserId, @TenantId, @UserName, @UserEmail, @Title, @Message,
                     N'Open', SYSUTCDATETIME());
                """,
                new
                {
                    sessionService.UserId,
                    sessionService.TenantId,
                    UserName = sessionService.UserName,
                    UserEmail = sessionService.UserEmail,
                    Title = request.Title.Trim(),
                    Message = request.Message.Trim()
                },
                cancellationToken: cancellationToken));

        var reference = $"VIQ-SUPPORT-{ticketId:D6}";
        var recipient = configuration["Support:EmailAddress"];
        if (string.IsNullOrWhiteSpace(recipient))
        {
            logger.LogError(
                "Support ticket {TicketId} was stored, but Support:EmailAddress is not configured.",
                ticketId);
            return new(ticketId, reference, false,
                $"Ticket {reference} was opened, but its email notification could not be sent. Please contact support.");
        }

        var emailAttachments = attachments.Select(attachment => new EmailReportAttachment(
            Path.GetFileName(attachment.FileName),
            GetContentType(attachment.FileName),
            attachment.Content)).ToArray();
        var delivery = await emailReportingService.SendReportAsync(
            new EmailReportRequest
            {
                RecipientAddress = recipient,
                SubjectTitle = $"Support ticket {reference}: {request.Title.Trim()}",
                MessageBodyText = BuildTicketEmail(
                    request,
                    reference,
                    sessionService.UserName,
                    sessionService.UserEmail),
                Attachments = emailAttachments
            },
            cancellationToken);

        if (!delivery.Succeeded)
        {
            logger.LogError(
                "Support ticket {TicketId} was stored, but email delivery failed with category {Category}.",
                ticketId,
                delivery.ErrorCategory);
            return new(ticketId, reference, false,
                $"Ticket {reference} was opened, but its email notification could not be sent. Please contact support.");
        }

        await connection.ExecuteAsync(
            new CommandDefinition(
                """
                UPDATE dbo.SupportTickets
                SET EmailSentAtUtc = SYSUTCDATETIME()
                WHERE SupportTicketId = @TicketId;
                """,
                new { TicketId = ticketId },
                cancellationToken: cancellationToken));

        return new(ticketId, reference, true,
            $"Ticket {reference} has been opened and sent to the support centre.");
    }

    private async Task<SupportMessage> InsertMessageAsync(
        IDbConnection connection,
        long conversationId,
        long senderUserId,
        string senderName,
        bool isConsultant,
        string message,
        DateTime sentAtUtc,
        CancellationToken cancellationToken,
        string? conversationUpdateSql = null,
        IDbTransaction? transaction = null)
    {
        if (connection.State != ConnectionState.Open)
            connection.Open();
        var ownsTransaction = transaction is null;
        transaction ??= connection.BeginTransaction();
        try
        {
            var userId = await connection.ExecuteScalarAsync<long>(
                new CommandDefinition(
                    """
                    SELECT UserId
                    FROM dbo.SupportConversations
                    WHERE SupportConversationId = @ConversationId;
                    """,
                    new { ConversationId = conversationId },
                    transaction,
                    cancellationToken: cancellationToken));
            var assignedConsultantId = await connection.ExecuteScalarAsync<long?>(
                new CommandDefinition(
                    """
                    SELECT ConsultantUserId
                    FROM dbo.SupportConversations
                    WHERE SupportConversationId = @ConversationId;
                    """,
                    new { ConversationId = conversationId },
                    transaction,
                    cancellationToken: cancellationToken));
            var recipientUserId = isConsultant ? userId : assignedConsultantId ?? userId;

            if (conversationUpdateSql is not null)
            {
                var updated = await connection.ExecuteAsync(
                    new CommandDefinition(
                        conversationUpdateSql,
                        new { ConversationId = conversationId, SenderUserId = senderUserId, SentAtUtc = sentAtUtc },
                        transaction,
                        cancellationToken: cancellationToken));
                if (updated != 1)
                    throw new InvalidOperationException("This conversation is closed or unavailable.");
            }

            var messageId = await connection.ExecuteScalarAsync<long>(
                new CommandDefinition(
                    """
                    INSERT INTO dbo.tblNotificationItem
                        (SenderId, RecipientId, Title, Message, IsRead, IsTask,
                         TaskStatus, CreatedDate, Active, CreatedBy,
                         SupportConversationId, SupportSenderName, IsSupportMessage)
                    OUTPUT INSERTED.NotificationItemId
                    VALUES
                        (@SenderUserId, @RecipientUserId, N'Support chat', @MessageText,
                         0, 0, 0, @SentAtUtc, 1, @SenderUserId,
                         @ConversationId, @SenderName, 1);
                    """,
                    new
                    {
                        ConversationId = conversationId,
                        SenderUserId = senderUserId,
                        RecipientUserId = recipientUserId,
                        SenderName = senderName,
                        MessageText = message,
                        SentAtUtc = sentAtUtc
                    },
                    transaction,
                    cancellationToken: cancellationToken));

            await connection.ExecuteAsync(
                new CommandDefinition(
                    """
                    UPDATE dbo.SupportConversations
                    SET LastMessage = @MessageText, LastMessageAtUtc = @SentAtUtc
                    WHERE SupportConversationId = @ConversationId;
                    """,
                    new { ConversationId = conversationId, MessageText = message, SentAtUtc = sentAtUtc },
                    transaction,
                    cancellationToken: cancellationToken));

            if (ownsTransaction)
                transaction.Commit();

            return new SupportMessage
            {
                SupportMessageId = messageId,
                SupportConversationId = conversationId,
                SenderUserId = senderUserId,
                SenderName = senderName,
                IsConsultant = isConsultant,
                MessageText = message,
                SentAtUtc = sentAtUtc
            };
        }
        catch
        {
            if (ownsTransaction)
                transaction.Rollback();
            throw;
        }
    }

    private async Task EnsureConversationAccessAsync(
        IDbConnection connection,
        long conversationId,
        CancellationToken cancellationToken)
    {
        var permitted = await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(
                """
                SELECT CASE WHEN EXISTS
                (
                    SELECT 1
                    FROM dbo.SupportConversations c
                    WHERE c.SupportConversationId = @ConversationId
                      AND c.UserId = @UserId
                      AND c.TenantId = @TenantId
                ) OR EXISTS
                (
                    SELECT 1
                    FROM dbo.SupportConversations c
                    WHERE c.SupportConversationId = @ConversationId
                      AND c.ConsultantUserId = @UserId
                      AND EXISTS
                      (
                          SELECT 1
                          FROM dbo.tblApplicationUserRoles ur
                          INNER JOIN dbo.tblApplicationRoles r ON r.RoleId = ur.RoleId
                          WHERE ur.UserId = @UserId
                            AND r.NormalizedName = N'SUPPORT CONSULTANT'
                      )
                ) THEN 1 ELSE 0 END;
                """,
                new
                {
                    ConversationId = conversationId,
                    UserId = sessionService.UserId,
                    TenantId = sessionService.TenantId
                },
                cancellationToken: cancellationToken));
        if (permitted == 0)
            throw new UnauthorizedAccessException("You cannot access this support conversation.");
    }

    private async Task EnsureUserOwnsConversationAsync(
        IDbConnection connection,
        long conversationId,
        CancellationToken cancellationToken)
    {
        var ownsConversation = await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(
                """
                SELECT COUNT(1)
                FROM dbo.SupportConversations
                WHERE SupportConversationId = @ConversationId
                  AND UserId = @UserId
                  AND TenantId = @TenantId
                  AND Status IN (N'Waiting', N'Active');
                """,
                new
                {
                    ConversationId = conversationId,
                    UserId = sessionService.UserId,
                    TenantId = sessionService.TenantId
                },
                cancellationToken: cancellationToken));
        if (ownsConversation == 0)
            throw new InvalidOperationException("This conversation is closed, expired, or unavailable.");
    }

    private async Task EnsureConsultantAsync(
        IDbConnection connection,
        CancellationToken cancellationToken,
        IDbTransaction? transaction = null)
    {
        if (!await IsConsultantAsync(connection, cancellationToken, transaction))
            throw new UnauthorizedAccessException("The Support Consultant role is required.");
    }

    private async Task<bool> IsConsultantAsync(
        IDbConnection connection,
        CancellationToken cancellationToken,
        IDbTransaction? transaction = null)
    {
        EnsureSignedIn();
        var isConsultant = await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(
                """
                SELECT CASE WHEN EXISTS
                (
                    SELECT 1
                    FROM dbo.tblApplicationUserRoles ur
                    INNER JOIN dbo.tblApplicationRoles r ON r.RoleId = ur.RoleId
                    WHERE ur.UserId = @UserId
                      AND r.NormalizedName = N'SUPPORT CONSULTANT'
                ) THEN 1 ELSE 0 END;
                """,
                new { UserId = sessionService.UserId },
                transaction,
                cancellationToken: cancellationToken));
        return isConsultant == 1;
    }

    private static string ValidateMessage(string? message)
    {
        var text = message?.Trim() ?? string.Empty;
        if (text.Length is < 1 or > 4000)
            throw new ArgumentException("Messages must contain between 1 and 4,000 characters.");
        return text;
    }

    private static void ValidateTicket(
        SupportTicketRequest request,
        IReadOnlyList<SupportFileAttachment> attachments)
    {
        if (string.IsNullOrWhiteSpace(request.Title) || request.Title.Trim().Length > 160)
            throw new ArgumentException("A ticket title of up to 160 characters is required.");
        if (string.IsNullOrWhiteSpace(request.Message) || request.Message.Trim().Length > 10000)
            throw new ArgumentException("A ticket description of up to 10,000 characters is required.");
        if (attachments.Count > MaximumAttachments)
            throw new ArgumentException($"Attach no more than {MaximumAttachments} files.");

        var totalBytes = 0L;
        var allowedExtensions = new HashSet<string>(
            [".pdf", ".doc", ".docx", ".xls", ".xlsx", ".csv", ".txt", ".png", ".jpg", ".jpeg", ".zip"],
            StringComparer.OrdinalIgnoreCase);
        foreach (var attachment in attachments)
        {
            if (attachment is null
                || string.IsNullOrWhiteSpace(attachment.FileName)
                || string.IsNullOrWhiteSpace(attachment.ContentType)
                || attachment.Content is null
                || attachment.Content.Length == 0
                || attachment.Content.Length > MaximumAttachmentBytes
                || !allowedExtensions.Contains(Path.GetExtension(attachment.FileName)))
                throw new ArgumentException("Attachments must be supported document, image, text, or ZIP files no larger than 10 MB each.");

            totalBytes += attachment.Content.Length;
        }

        if (totalBytes > MaximumTotalAttachmentBytes)
            throw new ArgumentException("The combined attachment size cannot exceed 25 MB.");
    }

    private void EnsureSignedIn()
    {
        if (!sessionService.IsAuthenticated || sessionService.UserId <= 0 || sessionService.TenantId <= 0)
            throw new UnauthorizedAccessException("Sign in to use ViabilityIQ support.");
    }

    private static string BuildTicketEmail(
        SupportTicketRequest request,
        string reference,
        string userName,
        string userEmail)
    {
        static string Encode(string value) => WebUtility.HtmlEncode(value);
        var message = Encode(request.Message.Trim()).Replace("\r\n", "<br>").Replace("\n", "<br>");
        return $"""
            <h2>New ViabilityIQ support ticket</h2>
            <p><strong>Reference:</strong> {Encode(reference)}</p>
            <p><strong>Title:</strong> {Encode(request.Title.Trim())}</p>
            <p><strong>User:</strong> {Encode(userName)} ({Encode(userEmail)})</p>
            <p><strong>Description:</strong><br>{message}</p>
            """;
    }

    private static string GetContentType(string fileName) =>
        Path.GetExtension(fileName).ToLowerInvariant() switch
        {
            ".pdf" => "application/pdf",
            ".doc" => "application/msword",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ".xls" => "application/vnd.ms-excel",
            ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            ".csv" => "text/csv",
            ".txt" => "text/plain",
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".zip" => "application/zip",
            _ => "application/octet-stream"
        };

    private const string ConversationSelect = """
        SELECT c.SupportConversationId, c.UserId, c.TenantId, c.UserName,
               c.UserEmail, c.ConsultantUserId, c.ConsultantName, c.Status,
               c.CreatedAtUtc, c.LastUserActivityAtUtc, c.ClosedAtUtc,
               c.LastMessage, c.LastMessageAtUtc
        FROM dbo.SupportConversations c
        """;
}
