using System.Data;
using System.Security.Claims;
using System.Text.Json;
using Dapper;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using ViabilityIQ.Application.Interfaces;
using ViabilityIQ.Infrastructure.DbFactory;

namespace ViabilityIQ.Infrastructure.Repositories;

public sealed class ActivityLogWriter : IActivityLogWriter
{
    private const int MaximumActorNameLength = 200;
    private const int MaximumEntityNameLength = 300;
    private const int MaximumAssessmentNameLength = 300;
    private const int MaximumModuleLength = 100;
    private const int MaximumPageLength = 500;
    private const int MaximumRemarksLength = 1000;
    private const int MaximumUserAgentLength = 1000;
    private const int MaximumMetadataLength = 16000;

    private static readonly JsonSerializerOptions MetadataOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly IDbConnectionFactory _dbConnectionFactory;
    private readonly ISessionService _sessionService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<ActivityLogWriter> _logger;

    public ActivityLogWriter(
        IDbConnectionFactory dbConnectionFactory,
        ISessionService sessionService,
        IHttpContextAccessor httpContextAccessor,
        ILogger<ActivityLogWriter> logger)
    {
        _dbConnectionFactory = dbConnectionFactory;
        _sessionService = sessionService;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public async Task RecordAsync(
        ActivityLogWriteRequest request,
        IDbConnection? connection = null,
        IDbTransaction? transaction = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.EntityType))
        {
            throw new ArgumentException("An activity entity type is required.", nameof(request));
        }

        if (transaction != null && connection == null)
        {
            throw new ArgumentException(
                "A connection must be supplied when an activity transaction is supplied.",
                nameof(connection));
        }

        var context = _httpContextAccessor.HttpContext;
        var principal = context?.User;
        var userId = request.UserId ?? ResolveUserId(principal) ?? PositiveOrNull(_sessionService.UserId);
        var actorName = FirstPopulated(
            request.ActorName,
            principal?.Identity?.Name,
            _sessionService.UserName,
            "System");
        var assessmentId = request.AssessmentId ?? _sessionService.AssessmentId;
        var assessmentName = FirstPopulated(
            request.AssessmentName,
            _sessionService.CaseNumber,
            _sessionService.BusinessName);

        var activity = new
        {
            UserId = userId,
            ActorName = Truncate(actorName, MaximumActorNameLength),
            ActivityAction = request.Action.ToString(),
            EntityType = Truncate(request.EntityType, 100),
            request.EntityId,
            EntityName = Truncate(request.EntityName, MaximumEntityNameLength),
            AssessmentId = assessmentId,
            AssessmentName = Truncate(assessmentName, MaximumAssessmentNameLength),
            Module = Truncate(request.Module, MaximumModuleLength),
            Page = Truncate(request.Page ?? context?.Request.Path.Value, MaximumPageLength),
            IpAddress = Truncate(context?.Connection.RemoteIpAddress?.ToString(), 100),
            UserAgent = Truncate(
                context?.Request.Headers["User-Agent"].ToString(),
                MaximumUserAgentLength),
            CorrelationId = Truncate(context?.TraceIdentifier, 100),
            MetadataJson = SerializeMetadata(request.Metadata),
            Remarks = Truncate(request.Remarks, MaximumRemarksLength),
            CreatedDate = DateTime.UtcNow,
            CreatedBy = userId ?? 0,
            ModifiedDate = DateTime.UtcNow,
            ModifiedBy = userId ?? 0
        };

        const string sql = """
            INSERT INTO dbo.tblActivityLog
            (
                UserId,
                ActorName,
                ActivityAction,
                EntityType,
                EntityId,
                EntityName,
                AssessmentId,
                AssessmentName,
                Module,
                Page,
                IpAddress,
                UserAgent,
                CorrelationId,
                MetadataJson,
                Remarks,
                Active,
                CreatedDate,
                CreatedBy,
                ModifiedDate,
                ModifiedBy
            )
            VALUES
            (
                @UserId,
                @ActorName,
                @ActivityAction,
                @EntityType,
                @EntityId,
                @EntityName,
                @AssessmentId,
                @AssessmentName,
                @Module,
                @Page,
                @IpAddress,
                @UserAgent,
                @CorrelationId,
                @MetadataJson,
                @Remarks,
                1,
                @CreatedDate,
                @CreatedBy,
                @ModifiedDate,
                @ModifiedBy
            );
            """;

        var ownsConnection = connection == null;
        connection ??= _dbConnectionFactory.CreateConnection();

        try
        {
            if (connection.State != ConnectionState.Open)
            {
                connection.Open();
            }

            var command = new CommandDefinition(
                sql,
                activity,
                transaction,
                cancellationToken: cancellationToken);
            await connection.ExecuteAsync(command);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to record {Action} activity for {EntityType} {EntityId}",
                request.Action,
                request.EntityType,
                request.EntityId);
            throw;
        }
        finally
        {
            if (ownsConnection)
            {
                connection.Dispose();
            }
        }
    }

    private static long? ResolveUserId(ClaimsPrincipal? principal)
    {
        var rawUserId = principal?.FindFirstValue(ClaimTypes.NameIdentifier);
        return long.TryParse(rawUserId, out var userId) && userId > 0 ? userId : null;
    }

    private static long? PositiveOrNull(long value) => value > 0 ? value : null;

    private static string? FirstPopulated(params string?[] values) =>
        values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));

    private static string? Truncate(string? value, int maximumLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        return trimmed.Length <= maximumLength ? trimmed : trimmed[..maximumLength];
    }

    private static string? SerializeMetadata(object? metadata)
    {
        if (metadata == null)
        {
            return null;
        }

        var json = JsonSerializer.Serialize(metadata, MetadataOptions);
        if (json.Length > MaximumMetadataLength)
        {
            throw new ArgumentException(
                $"Activity metadata exceeds the {MaximumMetadataLength}-character limit.",
                nameof(metadata));
        }

        return json;
    }
}
