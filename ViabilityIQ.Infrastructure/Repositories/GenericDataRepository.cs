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
using ViabilityIQ.Shared.DataModels.SecurityDataModels;
using ViabilityIQ.Shared.SharedModels;

namespace ViabilityIQ.Infrastructure.Repositories;

public class GenericDataRepository<T> : IGenericDataRepository<T> where T : class
{
    private static readonly Regex SqlIdentifierPattern =
        new("^[A-Za-z_][A-Za-z0-9_]*$", RegexOptions.Compiled);

    private readonly IDbConnectionFactory _dbConnectionFactory;
    private readonly ISessionService _sessionService;
    private readonly IActivityLogWriter _activityLogWriter;
    private readonly ITenantAuthorizationService _tenantAuthorizationService;

    public GenericDataRepository(
        IDbConnectionFactory dbConnectionFactory,
        ISessionService sessionService,
        IActivityLogWriter activityLogWriter,
        ITenantAuthorizationService tenantAuthorizationService)
    {
        _dbConnectionFactory = dbConnectionFactory
            ?? throw new ArgumentNullException(nameof(dbConnectionFactory));
        _sessionService = sessionService
            ?? throw new ArgumentNullException(nameof(sessionService));
        _activityLogWriter = activityLogWriter
            ?? throw new ArgumentNullException(nameof(activityLogWriter));
        _tenantAuthorizationService = tenantAuthorizationService
            ?? throw new ArgumentNullException(nameof(tenantAuthorizationService));
    }

    public async Task<T?> GetByIdAsync(long id)
    {
        using var connection = _dbConnectionFactory.CreateConnection();
        var scope = await GetScopeAsync();
        if (scope is null)
        {
            return await connection.GetAsync<T>(id);
        }

        var sql = $"""
            SELECT e.*
            FROM {GetQuotedTableName()} e
            WHERE e.[{GetKeyColumnName()}] = @Id
              AND {BuildReadScopePredicate(scope.Value.Kind, "e")};
            """;
        return await connection.QuerySingleOrDefaultAsync<T>(
            sql,
            BuildScopeParameters(scope.Value.Context, new { Id = id }));
    }

    public async Task<IEnumerable<T>> GetAllAsync()
    {
        using var connection = _dbConnectionFactory.CreateConnection();
        var scope = await GetScopeAsync();
        var items = scope is null
            ? await connection.GetAllAsync<T>()
            : await connection.QueryAsync<T>(
                $"""
                SELECT e.*
                FROM {GetQuotedTableName()} e
                WHERE {BuildReadScopePredicate(scope.Value.Kind, "e")};
                """,
                BuildScopeParameters(scope.Value.Context));
        return SortIfSupported(items);
    }

    public async Task<IEnumerable<T>> GetAllAsync(Expression<Func<T, bool>> predicate)
    {
        ArgumentNullException.ThrowIfNull(predicate);

        var items = await GetAllAsync();
        return SortIfSupported(items.Where(predicate.Compile()));
    }

    public async Task<IEnumerable<T>> GetByFieldAsync(string fieldName, object fieldValue)
    {
        ValidateSqlIdentifier(fieldName, nameof(fieldName));

        using var connection = _dbConnectionFactory.CreateConnection();
        var scope = await GetScopeAsync();
        var predicates = new List<string> { $"e.[{fieldName}] = @Value" };
        if (scope is not null)
        {
            predicates.Add(BuildReadScopePredicate(scope.Value.Kind, "e"));
        }

        var sql = $"""
            SELECT e.*
            FROM {GetQuotedTableName()} e
            WHERE {string.Join(" AND ", predicates)};
            """;
        var parameters = scope is null
            ? new DynamicParameters(new { Value = fieldValue })
            : BuildScopeParameters(scope.Value.Context, new { Value = fieldValue });
        var result = await connection.QueryAsync<T>(sql, parameters);
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

        var predicates = properties.Select(
            property => $"e.[{property.Name}] = @{property.Name}").ToList();
        var scope = await GetScopeAsync();
        if (scope is not null)
        {
            predicates.Add(BuildReadScopePredicate(scope.Value.Kind, "e"));
        }

        var whereClause = string.Join(
            " AND ",
            predicates);

        using var connection = _dbConnectionFactory.CreateConnection();
        var sql = $"SELECT e.* FROM {GetQuotedTableName()} e WHERE {whereClause}";
        var parameters = scope is null
            ? new DynamicParameters(filters)
            : BuildScopeParameters(scope.Value.Context, filters);
        var result = await connection.QueryAsync<T>(sql, parameters);
        return SortIfSupported(result);
    }

    public async Task<bool> SaveAsync(T entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        var scope = await GetScopeAsync();
        if (scope is not null)
        {
            await _tenantAuthorizationService.EnsureCanCreateOperationalDataAsync();
        }

        using var connection = _dbConnectionFactory.CreateConnection();
        OpenConnection(connection);
        using var transaction = connection.BeginTransaction();

        try
        {
            var isCreate = entity is not IEntity identity || identity.Id == 0;
            var entityId = entity is IEntity existingIdentity ? existingIdentity.Id : 0;
            ExistingAudit? existingAudit = null;
            bool saved;

            if (scope is not null)
            {
                if (isCreate)
                {
                    await EnsureCreateScopeAsync(
                        connection,
                        transaction,
                        entity,
                        scope.Value);
                }
                else
                {
                    existingAudit = await EnsureExistingRecordWriteAccessAsync(
                        connection,
                        transaction,
                        entityId,
                        scope.Value,
                        delete: false);
                }
            }

            if (entity is ITenantEntity tenantEntity)
            {
                tenantEntity.TenantId = _sessionService.TenantId;
            }

            await ValidateForeignKeysAsync(connection, transaction, entity, scope);

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
                    if (existingAudit is not null)
                    {
                        auditable.CreatedBy = existingAudit.CreatedBy!.Value;
                        auditable.CreatedDate = existingAudit.CreatedDate!.Value;
                    }

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

        var scope = await GetScopeAsync();
        if (scope is not null)
        {
            await _tenantAuthorizationService.EnsureCanCreateOperationalDataAsync();
        }

        using var connection = _dbConnectionFactory.CreateConnection();
        OpenConnection(connection);
        using var transaction = connection.BeginTransaction();

        try
        {
            var entityId = entity is IEntity identity ? identity.Id : 0;
            if (scope is not null)
            {
                await EnsureExistingRecordWriteAccessAsync(
                    connection,
                    transaction,
                    entityId,
                    scope.Value,
                    delete: true);
            }

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

        var items = await GetAllAsync();
        return items.Count(predicate.Compile());
    }

    public async Task<int> CountAsync()
    {
        var items = await GetAllAsync();
        return items.Count();
    }

    public async Task<bool> AnyAsync(Expression<Func<T, bool>> predicate)
    {
        ArgumentNullException.ThrowIfNull(predicate);

        var items = await GetAllAsync();
        return items.Any(predicate.Compile());
    }

    private async Task<(ScopeKind Kind, TenantAccessContext Context)?> GetScopeAsync()
    {
        var kind = GetScopeKind();
        if (kind == ScopeKind.None)
        {
            return null;
        }

        await _tenantAuthorizationService.EnsureCanReadOperationalDataAsync();
        return (kind, await _tenantAuthorizationService.GetAccessContextAsync());
    }

    private static ScopeKind GetScopeKind()
    {
        if (typeof(ITenantEntity).IsAssignableFrom(typeof(T)))
        {
            return ScopeKind.DirectTenant;
        }

        if (typeof(T).GetProperty("AssessmentId") is not null)
        {
            return ScopeKind.AssessmentChild;
        }

        return ScopeKind.None;
    }

    private static string BuildReadScopePredicate(ScopeKind kind, string alias) =>
        kind switch
        {
            ScopeKind.DirectTenant => $"""
                {alias}.[TenantId] = @TenantId
                AND
                (
                    @CanReadAll = 1
                    OR {alias}.[CreatedBy] = @UserId
                    OR EXISTS
                    (
                        SELECT 1
                        FROM tblTenantRecordGrant grantRecord
                        WHERE grantRecord.TenantId = @TenantId
                          AND grantRecord.TenantMembershipId = @MembershipId
                          AND grantRecord.EntityType = @EntityType
                          AND grantRecord.EntityId = {alias}.[{GetKeyColumnName()}]
                          AND grantRecord.Active = 1
                          AND grantRecord.CanView = 1
                    )
                )
                """,
            ScopeKind.AssessmentChild => $"""
                EXISTS
                (
                    SELECT 1
                    FROM tblAssessments scopeAssessment
                    WHERE scopeAssessment.AssessmentId = {alias}.[AssessmentId]
                      AND scopeAssessment.TenantId = @TenantId
                      AND
                      (
                          @CanReadAll = 1
                          OR scopeAssessment.CreatedBy = @UserId
                          OR EXISTS
                          (
                              SELECT 1
                              FROM tblTenantRecordGrant grantRecord
                              WHERE grantRecord.TenantId = @TenantId
                                AND grantRecord.TenantMembershipId = @MembershipId
                                AND grantRecord.EntityType = 'Assessment'
                                AND grantRecord.EntityId = scopeAssessment.AssessmentId
                                AND grantRecord.Active = 1
                                AND grantRecord.CanView = 1
                          )
                      )
                )
                """,
            _ => "1 = 1"
        };

    private static string BuildWriteScopePredicate(
        ScopeKind kind,
        string alias,
        bool delete) =>
        kind switch
        {
            ScopeKind.DirectTenant => $"""
                {alias}.[TenantId] = @TenantId
                AND
                (
                    @CanWriteAll = 1
                    OR (@CanWriteScoped = 1 AND {alias}.[CreatedBy] = @UserId)
                    OR EXISTS
                    (
                        SELECT 1
                        FROM tblTenantRecordGrant grantRecord
                        WHERE grantRecord.TenantId = @TenantId
                          AND grantRecord.TenantMembershipId = @MembershipId
                          AND grantRecord.EntityType = @EntityType
                          AND grantRecord.EntityId = {alias}.[{GetKeyColumnName()}]
                          AND grantRecord.Active = 1
                          AND grantRecord.[{(delete ? "CanDelete" : "CanEdit")}] = 1
                    )
                )
                """,
            ScopeKind.AssessmentChild => $"""
                EXISTS
                (
                    SELECT 1
                    FROM tblAssessments scopeAssessment
                    WHERE scopeAssessment.AssessmentId = {alias}.[AssessmentId]
                      AND scopeAssessment.TenantId = @TenantId
                      AND
                      (
                          @CanWriteAll = 1
                          OR (@CanWriteScoped = 1 AND scopeAssessment.CreatedBy = @UserId)
                          OR EXISTS
                          (
                              SELECT 1
                              FROM tblTenantRecordGrant grantRecord
                              WHERE grantRecord.TenantId = @TenantId
                                AND grantRecord.TenantMembershipId = @MembershipId
                                AND grantRecord.EntityType = 'Assessment'
                                AND grantRecord.EntityId = scopeAssessment.AssessmentId
                                AND grantRecord.Active = 1
                                AND grantRecord.[{(delete ? "CanDelete" : "CanEdit")}] = 1
                          )
                      )
                )
                """,
            _ => "1 = 1"
        };

    private static DynamicParameters BuildScopeParameters(
        TenantAccessContext context,
        object? values = null,
        string? entityType = null)
    {
        var parameters = values is null
            ? new DynamicParameters()
            : new DynamicParameters(values);
        parameters.Add("TenantId", context.TenantId);
        parameters.Add("UserId", context.UserId);
        parameters.Add("MembershipId", context.MembershipId);
        parameters.Add("CanReadAll", context.CanReadAllOperationalRecords);
        parameters.Add("CanWriteAll", context.CanWriteAllOperationalRecords);
        parameters.Add("CanWriteScoped", context.CanWriteScopedOperationalRecords);
        parameters.Add("EntityType", entityType ?? typeof(T).Name);
        return parameters;
    }

    private async Task EnsureCreateScopeAsync(
        IDbConnection connection,
        IDbTransaction transaction,
        T entity,
        (ScopeKind Kind, TenantAccessContext Context) scope)
    {
        if (!scope.Context.CanWriteAllOperationalRecords
            && !scope.Context.CanWriteScopedOperationalRecords)
        {
            throw new UnauthorizedAccessException(
                $"You are not permitted to create {typeof(T).Name} records.");
        }

        if (scope.Kind != ScopeKind.AssessmentChild)
        {
            return;
        }

        var assessmentId = ReadLongProperty(entity, "AssessmentId")
            ?? throw new InvalidOperationException(
                $"{typeof(T).Name} requires a valid AssessmentId.");
        await EnsureAssessmentWriteAccessAsync(
            connection,
            transaction,
            assessmentId,
            scope.Context,
            delete: false);
    }

    private async Task<ExistingAudit?> EnsureExistingRecordWriteAccessAsync(
        IDbConnection connection,
        IDbTransaction transaction,
        long entityId,
        (ScopeKind Kind, TenantAccessContext Context) scope,
        bool delete)
    {
        if (entityId <= 0)
        {
            throw new InvalidOperationException(
                $"A valid {typeof(T).Name} identifier is required.");
        }

        if (delete
            && !scope.Context.CanWriteAllOperationalRecords
            && !scope.Context.CanDeleteScopedOperationalRecords)
        {
            throw new UnauthorizedAccessException(
                $"You are not permitted to delete {typeof(T).Name} records.");
        }

        var auditColumns = typeof(IAuditableEntity).IsAssignableFrom(typeof(T))
            ? "e.[CreatedBy], e.[CreatedDate]"
            : "CAST(NULL AS BIGINT) AS CreatedBy, CAST(NULL AS DATETIME2) AS CreatedDate";
        var sql = $"""
            SELECT TOP (1) {auditColumns}
            FROM {GetQuotedTableName()} e
            WHERE e.[{GetKeyColumnName()}] = @Id
              AND {BuildWriteScopePredicate(scope.Kind, "e", delete)};
            """;
        var existing = await connection.QuerySingleOrDefaultAsync<ExistingAudit>(
            sql,
            BuildScopeParameters(scope.Context, new { Id = entityId }),
            transaction);
        if (existing is null)
        {
            throw new UnauthorizedAccessException(
                $"The requested {typeof(T).Name} record is outside your authorised scope.");
        }

        return existing.CreatedBy.HasValue && existing.CreatedDate.HasValue
            ? existing
            : null;
    }

    private static async Task EnsureAssessmentWriteAccessAsync(
        IDbConnection connection,
        IDbTransaction transaction,
        long assessmentId,
        TenantAccessContext context,
        bool delete)
    {
        var permissionColumn = delete ? "CanDelete" : "CanEdit";
        var sql = $"""
            SELECT COUNT_BIG(1)
            FROM tblAssessments assessment
            WHERE assessment.AssessmentId = @AssessmentId
              AND assessment.TenantId = @TenantId
              AND
              (
                  @CanWriteAll = 1
                  OR (@CanWriteScoped = 1 AND assessment.CreatedBy = @UserId)
                  OR EXISTS
                  (
                      SELECT 1
                      FROM tblTenantRecordGrant grantRecord
                      WHERE grantRecord.TenantId = @TenantId
                        AND grantRecord.TenantMembershipId = @MembershipId
                        AND grantRecord.EntityType = 'Assessment'
                        AND grantRecord.EntityId = assessment.AssessmentId
                        AND grantRecord.Active = 1
                        AND grantRecord.[{permissionColumn}] = 1
                  )
              );
            """;
        var count = await connection.ExecuteScalarAsync<long>(
            sql,
            BuildScopeParameters(context, new { AssessmentId = assessmentId }),
            transaction);
        if (count == 0)
        {
            throw new UnauthorizedAccessException(
                "The selected assessment is outside your authorised scope.");
        }
    }

    private async Task ValidateForeignKeysAsync(
        IDbConnection connection,
        IDbTransaction transaction,
        T entity,
        (ScopeKind Kind, TenantAccessContext Context)? scope)
    {
        if (scope is not null)
        {
            var businessId = ReadLongProperty(entity, "BusinessId");
            if (businessId.HasValue)
            {
                await EnsureTenantReferenceAsync(
                    connection, transaction, "tblBusiness", "BusinessId",
                    TenantRecordTypes.Business, businessId.Value, scope.Value.Context);
            }

            var clientId = ReadLongProperty(entity, "ClientId");
            if (clientId.HasValue)
            {
                await EnsureTenantReferenceAsync(
                    connection, transaction, "tblClient", "ClientId",
                    TenantRecordTypes.Client, clientId.Value, scope.Value.Context);
            }
        }

        foreach (var reference in ActiveReferenceFields)
        {
            if (reference.Key.Equals(
                    GetKeyColumnName(),
                    StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var value = ReadLongProperty(entity, reference.Key);
            if (!value.HasValue)
            {
                continue;
            }

            var count = await connection.ExecuteScalarAsync<long>(
                $"""
                SELECT COUNT_BIG(1)
                FROM [{reference.Value.Table}]
                WHERE [{reference.Value.Key}] = @Id
                  AND [Active] = 1;
                """,
                new { Id = value.Value },
                transaction);
            if (count == 0)
            {
                throw new InvalidOperationException(
                    $"The selected {reference.Key} is inactive or unavailable.");
            }
        }
    }

    private static async Task EnsureTenantReferenceAsync(
        IDbConnection connection,
        IDbTransaction transaction,
        string table,
        string key,
        string entityType,
        long entityId,
        TenantAccessContext context)
    {
        var sql = $"""
            SELECT COUNT_BIG(1)
            FROM [{table}] referenceRecord
            WHERE referenceRecord.[{key}] = @Id
              AND referenceRecord.TenantId = @TenantId
              AND referenceRecord.Active = 1
              AND
              (
                  @CanReadAll = 1
                  OR referenceRecord.CreatedBy = @UserId
                  OR EXISTS
                  (
                      SELECT 1
                      FROM tblTenantRecordGrant grantRecord
                      WHERE grantRecord.TenantId = @TenantId
                        AND grantRecord.TenantMembershipId = @MembershipId
                        AND grantRecord.EntityType = @EntityType
                        AND grantRecord.EntityId = referenceRecord.[{key}]
                        AND grantRecord.Active = 1
                        AND grantRecord.CanView = 1
                  )
              );
            """;
        var count = await connection.ExecuteScalarAsync<long>(
            sql,
            BuildScopeParameters(context, new { Id = entityId }, entityType),
            transaction);
        if (count == 0)
        {
            throw new InvalidOperationException(
                $"The selected {entityType} is inactive or outside your authorised scope.");
        }
    }

    private static readonly IReadOnlyDictionary<string, (string Table, string Key)>
        ActiveReferenceFields =
            new Dictionary<string, (string Table, string Key)>(StringComparer.Ordinal)
            {
                ["AssessmentTypeId"] = ("tblAssessmentType", "AssessmentTypeId"),
                ["AssetCategoryId"] = ("tblAssetCategory", "AssetCategoryId"),
                ["AssetTypeId"] = ("tblAssetType", "AssetTypeId"),
                ["BankId"] = ("tblBank", "BankId"),
                ["BusinessCategoryId"] = ("tblBusinessCategories", "BusinessCategoryId"),
                ["BusinessSectorId"] = ("tblBusinessSector", "BusinessSectorId"),
                ["ClientCategoryId"] = ("tblClientCategories", "ClientCategoryId"),
                ["ClientTypeId"] = ("tblClientType", "ClientTypeId"),
                ["ExpenseItemId"] = ("tblExpenseItems", "ExpenseItemId"),
                ["ExpenseTypeId"] = ("tblExpenseType", "ExpenseTypeId"),
                ["GenderId"] = ("tblGender", "GenderId"),
                ["IncomeTypeId"] = ("tblIncomeType", "IncomeTypeId"),
                ["LoanTypeId"] = ("tblLoanType", "LoanTypeId"),
                ["ProductCategoryId"] = ("tblProductCategory", "ProductCategoryId"),
                ["ProvinceId"] = ("tblProvince", "ProvinceId"),
                ["RaceId"] = ("tblRace", "RaceId")
            };

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

    private static string GetKeyColumnName()
    {
        var keyProperty = typeof(T).GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .FirstOrDefault(property => property.GetCustomAttributes().Any(attribute =>
                attribute.GetType().Name is "KeyAttribute" or "ExplicitKeyAttribute"))
            ?? typeof(T).GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .FirstOrDefault(property =>
                    property.Name.Equals(
                        $"{typeof(T).Name}Id",
                        StringComparison.OrdinalIgnoreCase));

        return keyProperty?.Name
            ?? throw new InvalidOperationException(
                $"{typeof(T).Name} does not define a supported key property.");
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

    private enum ScopeKind
    {
        None,
        DirectTenant,
        AssessmentChild
    }

    private sealed class ExistingAudit
    {
        public long? CreatedBy { get; init; }
        public DateTime? CreatedDate { get; init; }
    }
}
