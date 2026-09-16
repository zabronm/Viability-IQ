using System.Data;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.RegularExpressions;
using Dapper;
using Dapper.Contrib.Extensions;
using ViabilityIQ.Application.Interfaces;
using ViabilityIQ.Infrastructure.DbFactory;
using ViabilityIQ.Shared.DataModels;
using ViabilityIQ.Shared.DataModelsInterfaces;
using ViabilityIQ.Shared.SharedModels;

namespace ViabilityIQ.Infrastructure.Repositories;

public class GenericDataRepository<T> : IGenericDataRepository<T> where T : class
{
    private static readonly Regex SqlIdentifierPattern =
        new("^[A-Za-z_][A-Za-z0-9_]*$", RegexOptions.Compiled);

    private readonly IDbConnectionFactory _dbConnectionFactory;
    private readonly ISessionService _sessionService;
    private readonly IActivityLogWriter _activityLogWriter;

    public GenericDataRepository(
        IDbConnectionFactory dbConnectionFactory,
        ISessionService sessionService,
        IActivityLogWriter activityLogWriter)
    {
        _dbConnectionFactory = dbConnectionFactory
            ?? throw new ArgumentNullException(nameof(dbConnectionFactory));
        _sessionService = sessionService
            ?? throw new ArgumentNullException(nameof(sessionService));
        _activityLogWriter = activityLogWriter
            ?? throw new ArgumentNullException(nameof(activityLogWriter));
    }

    public async Task<T?> GetByIdAsync(long id)
    {
        using var connection = _dbConnectionFactory.CreateConnection();
        return await connection.GetAsync<T>(id);
    }

    public async Task<IEnumerable<T>> GetAllAsync()
    {
        using var connection = _dbConnectionFactory.CreateConnection();
        var items = await connection.GetAllAsync<T>();
        return SortIfSupported(items);
    }

    public async Task<IEnumerable<T>> GetAllAsync(Expression<Func<T, bool>> predicate)
    {
        ArgumentNullException.ThrowIfNull(predicate);

        using var connection = _dbConnectionFactory.CreateConnection();
        var items = await connection.GetAllAsync<T>();
        return SortIfSupported(items.Where(predicate.Compile()));
    }

    public async Task<IEnumerable<T>> GetByFieldAsync(string fieldName, object fieldValue)
    {
        ValidateSqlIdentifier(fieldName, nameof(fieldName));

        using var connection = _dbConnectionFactory.CreateConnection();
        var sql = $"SELECT * FROM {GetQuotedTableName()} WHERE [{fieldName}] = @Value";
        var result = await connection.QueryAsync<T>(sql, new { Value = fieldValue });
        return SortIfSupported(result);
    }

    public async Task<IEnumerable<T>> GetByFieldsAsync(object filters)
    {
        ArgumentNullException.ThrowIfNull(filters);

        var properties = filters.GetType().GetProperties();
        if (properties.Length == 0)
        {
            throw new ArgumentException(
                "Filters object must have at least one property.",
                nameof(filters));
        }

        foreach (var property in properties)
        {
            ValidateSqlIdentifier(property.Name, nameof(filters));
        }

        var whereClause = string.Join(
            " AND ",
            properties.Select(property => $"[{property.Name}] = @{property.Name}"));

        using var connection = _dbConnectionFactory.CreateConnection();
        var sql = $"SELECT * FROM {GetQuotedTableName()} WHERE {whereClause}";
        var result = await connection.QueryAsync<T>(sql, filters);
        return SortIfSupported(result);
    }

    public async Task<bool> SaveAsync(T entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        using var connection = _dbConnectionFactory.CreateConnection();
        OpenConnection(connection);
        using var transaction = connection.BeginTransaction();

        try
        {
            var isCreate = entity is not IEntity identity || identity.Id == 0;
            var entityId = entity is IEntity existingIdentity ? existingIdentity.Id : 0;
            bool saved;

            if (entity is IAuditableEntity auditable)
            {
                if (isCreate)
                {
                    auditable.CreatedDate = DateTime.UtcNow;
                    auditable.CreatedBy = _sessionService.UserId;
                    auditable.Active = true;

                    entityId = await connection.InsertAsync(entity, transaction);
                    saved = entityId > 0;
                }
                else
                {
                    auditable.ModifiedDate = DateTime.UtcNow;
                    auditable.ModifiedBy = _sessionService.UserId;
                    saved = await connection.UpdateAsync(entity, transaction);
                }
            }
            else
            {
                saved = await connection.UpdateAsync(entity, transaction);
            }

            if (!saved)
            {
                transaction.Rollback();
                return false;
            }

            if (!IsActivityLogEntity())
            {
                await _activityLogWriter.RecordAsync(
                    BuildWriteRequest(
                        isCreate ? ActivityAction.Create : ActivityAction.Update,
                        entity,
                        entityId),
                    connection,
                    transaction);
            }

            transaction.Commit();
            return true;
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public async Task<bool> DeleteAsync(T entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        using var connection = _dbConnectionFactory.CreateConnection();
        OpenConnection(connection);
        using var transaction = connection.BeginTransaction();

        try
        {
            var entityId = entity is IEntity identity ? identity.Id : 0;
            var deleted = await connection.DeleteAsync(entity, transaction);

            if (!deleted)
            {
                transaction.Rollback();
                return false;
            }

            if (!IsActivityLogEntity())
            {
                await _activityLogWriter.RecordAsync(
                    BuildWriteRequest(ActivityAction.Delete, entity, entityId),
                    connection,
                    transaction);
            }

            transaction.Commit();
            return true;
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public async Task<int> CountAsync(Expression<Func<T, bool>> predicate)
    {
        ArgumentNullException.ThrowIfNull(predicate);

        using var connection = _dbConnectionFactory.CreateConnection();
        var items = await connection.GetAllAsync<T>();
        return items.Count(predicate.Compile());
    }

    public async Task<int> CountAsync()
    {
        using var connection = _dbConnectionFactory.CreateConnection();
        var items = await connection.GetAllAsync<T>();
        return items.Count();
    }

    public async Task<bool> AnyAsync(Expression<Func<T, bool>> predicate)
    {
        ArgumentNullException.ThrowIfNull(predicate);

        using var connection = _dbConnectionFactory.CreateConnection();
        var items = await connection.GetAllAsync<T>();
        return items.Any(predicate.Compile());
    }

    private ActivityLogWriteRequest BuildWriteRequest(
        ActivityAction action,
        T entity,
        long entityId)
    {
        var assessmentId = ReadLongProperty(entity, "AssessmentId")
            ?? _sessionService.AssessmentId;
        var entityName = TryGetDisplayName(entity)
            ?? ReadStringProperty(entity, "Name")
            ?? ReadStringProperty(entity, $"{typeof(T).Name}Name");

        return new ActivityLogWriteRequest
        {
            Action = action,
            EntityType = typeof(T).Name,
            EntityId = entityId > 0 ? entityId : null,
            EntityName = entityName,
            AssessmentId = assessmentId,
            AssessmentName = assessmentId.HasValue ? _sessionService.CaseNumber : null,
            Module = typeof(T).Namespace,
            Page = _sessionService.CurrentPage,
            Remarks = $"{action} {SplitPascalCase(typeof(T).Name)} record."
        };
    }

    private static IEnumerable<T> SortIfSupported(IEnumerable<T> items)
    {
        if (!typeof(ISortableEntity).IsAssignableFrom(typeof(T)))
        {
            return items.ToList();
        }

        return items.Cast<ISortableEntity>()
            .OrderBy(item => item.DisplayName)
            .Cast<T>()
            .ToList();
    }

    private static string GetQuotedTableName()
    {
        var tableAttribute = typeof(T).GetCustomAttribute<TableAttribute>();
        var tableName = tableAttribute?.Name ?? typeof(T).Name;
        var segments = tableName.Split('.', StringSplitOptions.RemoveEmptyEntries);

        foreach (var segment in segments)
        {
            ValidateSqlIdentifier(segment, nameof(tableName));
        }

        return string.Join(".", segments.Select(segment => $"[{segment}]"));
    }

    private static void ValidateSqlIdentifier(string identifier, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(identifier) || !SqlIdentifierPattern.IsMatch(identifier))
        {
            throw new ArgumentException($"Invalid SQL identifier '{identifier}'.", parameterName);
        }
    }

    private static void OpenConnection(IDbConnection connection)
    {
        if (connection.State != ConnectionState.Open)
        {
            connection.Open();
        }
    }

    private static bool IsActivityLogEntity() => typeof(T) == typeof(ActivityLog);

    private static long? ReadLongProperty(T entity, string propertyName)
    {
        var value = typeof(T).GetProperty(propertyName)?.GetValue(entity);
        return value switch
        {
            long longValue when longValue > 0 => longValue,
            int intValue when intValue > 0 => intValue,
            _ => null
        };
    }

    private static string? ReadStringProperty(T entity, string propertyName) =>
        typeof(T).GetProperty(propertyName)?.GetValue(entity)?.ToString();

    private static string? TryGetDisplayName(T entity)
    {
        if (entity is not ISortableEntity sortable)
        {
            return null;
        }

        try
        {
            return sortable.DisplayName;
        }
        catch (NotImplementedException)
        {
            return null;
        }
    }

    private static string SplitPascalCase(string value) =>
        Regex.Replace(value, "(?<=[a-z])(?=[A-Z])", " ");
}
