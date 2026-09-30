using Dapper;
using System.Reflection;
using System.Text.RegularExpressions;
using ViabilityIQ.Application.Interfaces;
using ViabilityIQ.Infrastructure.DbFactory;
using ViabilityIQ.Shared.DataModels.SecurityDataModels;
using ViabilityIQ.Shared.SharedModels;

namespace ViabilityIQ.Infrastructure.Repositories
{
    public class ReadOnlyRepository<TDto, TId> : IReadOnlyRepository<TDto, TId> where TDto : class
    {
        private const int MaximumPageSize = 500;
        private static readonly Regex SqlIdentifierPattern =
            new("^[A-Za-z_][A-Za-z0-9_]*$", RegexOptions.Compiled);
        private static readonly IReadOnlyDictionary<string, string> QueryableFields =
            typeof(TDto).GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Where(property => property.CanRead && IsQueryableType(property.PropertyType))
                .ToDictionary(property => property.Name, property => property.Name,
                    StringComparer.OrdinalIgnoreCase);

        private readonly IDbConnectionFactory _dbConnectionFactory;
        private readonly ITenantAuthorizationService _tenantAuthorizationService;
        private readonly string _tableName;

        public ReadOnlyRepository(
            IDbConnectionFactory dbConnectionFactory,
            ITenantAuthorizationService tenantAuthorizationService)
        {

            _dbConnectionFactory = dbConnectionFactory
                ?? throw new ArgumentNullException(nameof(dbConnectionFactory));
            _tenantAuthorizationService = tenantAuthorizationService
                ?? throw new ArgumentNullException(nameof(tenantAuthorizationService));

            var tableName = typeof(TDto).GetCustomAttribute<TableNameAttribute>()?.Name
                ?? typeof(TDto).GetCustomAttribute<Dapper.Contrib.Extensions.TableAttribute>()?.Name;
            if (string.IsNullOrWhiteSpace(tableName))
            {
                throw new InvalidOperationException(
                    $"Architectural Error: The DTO type '{typeof(TDto).Name}' must be decorated with a [TableName] or [Table] attribute.");
            }
            _tableName = QuoteMultipartIdentifier(tableName);
        }

        public async Task<IEnumerable<TDto>> GetAllAsync()
        {
            using var connection = _dbConnectionFactory.CreateConnection();
            var scope = await GetScopeAsync();
            var where = scope is null
                ? string.Empty
                : $"WHERE {BuildScopePredicate(scope.Value.Metadata)}";
            var sql = $"SELECT sourceRecord.* FROM {_tableName} sourceRecord {where};";
            return await connection.QueryAsync<TDto>(
                sql,
                scope is null ? null : BuildScopeParameters(scope.Value.Context));
        }

        public async Task<IEnumerable<TDto>> GetListByIdAsync(string idFieldName, TId idValue)
        {
            var field = GetValidatedField(idFieldName);
            using var connection = _dbConnectionFactory.CreateConnection();
            var scope = await GetScopeAsync();
            var predicates = new List<string> { $"sourceRecord.[{field}] = @Id" };
            if (scope is not null)
            {
                predicates.Add(BuildScopePredicate(scope.Value.Metadata));
            }

            var sql = $"""
                SELECT sourceRecord.*
                FROM {_tableName} sourceRecord
                WHERE {string.Join(" AND ", predicates)};
                """;
            var parameters = scope is null
                ? new DynamicParameters(new { Id = idValue })
                : BuildScopeParameters(scope.Value.Context, new { Id = idValue });
            return await connection.QueryAsync<TDto>(sql, parameters);
        }


        public async Task<TDto?> GetFirstOrDefaultAsync(string idFieldName, TId idValue)
        {
            var field = GetValidatedField(idFieldName);
            using var connection = _dbConnectionFactory.CreateConnection();
            var scope = await GetScopeAsync();
            var predicates = new List<string> { $"sourceRecord.[{field}] = @Id" };
            if (scope is not null)
            {
                predicates.Add(BuildScopePredicate(scope.Value.Metadata));
            }

            var sql = $"""
                SELECT TOP 1 sourceRecord.*
                FROM {_tableName} sourceRecord
                WHERE {string.Join(" AND ", predicates)};
                """;
            var parameters = scope is null
                ? new DynamicParameters(new { Id = idValue })
                : BuildScopeParameters(scope.Value.Context, new { Id = idValue });
            return await connection.QueryFirstOrDefaultAsync<TDto>(sql, parameters);
        }

        public async Task<DataTablePage<TDto>> GetPageAsync(
            DataTableQuery query,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(query);

            var pageNumber = Math.Max(1, query.PageNumber);
            var pageSize = Math.Clamp(query.PageSize, 1, MaximumPageSize);
            var offset = checked((pageNumber - 1) * pageSize);
            var searchFields = query.SearchFields
                .Where(field => !string.IsNullOrWhiteSpace(field))
                .Select(GetValidatedField)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
            var sortField = string.IsNullOrWhiteSpace(query.SortField)
                ? GetDefaultSortField()
                : GetValidatedField(query.SortField);

            var predicates = new List<string>();
            var parameters = new DynamicParameters();
            parameters.Add("Offset", offset);
            parameters.Add("PageSize", pageSize);
            var scope = await GetScopeAsync(cancellationToken);
            if (scope is not null)
            {
                AddScopeParameters(parameters, scope.Value.Context);
                predicates.Add(BuildScopePredicate(scope.Value.Metadata));
            }

            if (!string.IsNullOrWhiteSpace(query.SearchText) && searchFields.Length > 0)
            {
                parameters.Add("SearchPattern", $"%{EscapeLike(query.SearchText.Trim())}%");
                predicates.Add("(" + string.Join(" OR ", searchFields.Select(field =>
                    $"CONVERT(nvarchar(4000), [{field}]) LIKE @SearchPattern ESCAPE '\\'")) + ")");
            }

            var filterIndex = 0;
            foreach (var filter in query.Filters.Where(item => !string.IsNullOrWhiteSpace(item.Value)))
            {
                var field = GetValidatedField(filter.Key);
                var parameterName = $"Filter{filterIndex++}";
                parameters.Add(parameterName, $"%{EscapeLike(filter.Value.Trim())}%");
                predicates.Add(
                    $"CONVERT(nvarchar(4000), [{field}]) LIKE @{parameterName} ESCAPE '\\'");
            }

            var where = predicates.Count == 0
                ? string.Empty
                : $"WHERE {string.Join(" AND ", predicates)}";
            var direction = query.SortDescending ? "DESC" : "ASC";
            var tieBreaker = GetDefaultSortField();
            var secondarySort = string.Equals(sortField, tieBreaker, StringComparison.OrdinalIgnoreCase)
                ? string.Empty
                : $", [{tieBreaker}] ASC";

            var sql = $"""
                SELECT *
                FROM {_tableName} sourceRecord
                {where}
                ORDER BY [{sortField}] {direction}{secondarySort}
                OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;

                SELECT COUNT_BIG(1)
                FROM {_tableName} sourceRecord
                {where};
                """;

            using var connection = _dbConnectionFactory.CreateConnection();
            var command = new CommandDefinition(
                sql, parameters, cancellationToken: cancellationToken);
            using var results = await connection.QueryMultipleAsync(command);
            var items = (await results.ReadAsync<TDto>()).ToList();
            var totalCount = checked((int)await results.ReadSingleAsync<long>());

            return new DataTablePage<TDto>
            {
                Items = items,
                TotalCount = totalCount
            };
        }

        private async Task<(ScopeMetadata Metadata, TenantAccessContext Context)?> GetScopeAsync(
            CancellationToken cancellationToken = default)
        {
            if (!ScopeRegistry.TryGetValue(typeof(TDto).Name, out var metadata))
            {
                return null;
            }

            await _tenantAuthorizationService.EnsureCanReadOperationalDataAsync(cancellationToken);
            return (
                metadata,
                await _tenantAuthorizationService.GetAccessContextAsync(cancellationToken));
        }

        private static string BuildScopePredicate(ScopeMetadata metadata) => $"""
            EXISTS
            (
                SELECT 1
                FROM [{metadata.BackingTable}] scopeRecord
                WHERE scopeRecord.[{metadata.KeyField}] =
                      sourceRecord.[{metadata.KeyField}]
                  AND scopeRecord.TenantId = @TenantId
                  AND
                  (
                      @CanReadAll = 1
                      OR scopeRecord.CreatedBy = @UserId
                      OR EXISTS
                      (
                          SELECT 1
                          FROM tblTenantRecordGrant grantRecord
                          WHERE grantRecord.TenantId = @TenantId
                            AND grantRecord.TenantMembershipId = @MembershipId
                            AND grantRecord.EntityType = '{metadata.EntityType}'
                            AND grantRecord.EntityId = scopeRecord.[{metadata.KeyField}]
                            AND grantRecord.Active = 1
                            AND grantRecord.CanView = 1
                      )
                  )
            )
            """;

        private static DynamicParameters BuildScopeParameters(
            TenantAccessContext context,
            object? values = null)
        {
            var parameters = values is null
                ? new DynamicParameters()
                : new DynamicParameters(values);
            AddScopeParameters(parameters, context);
            return parameters;
        }

        private static void AddScopeParameters(
            DynamicParameters parameters,
            TenantAccessContext context)
        {
            parameters.Add("TenantId", context.TenantId);
            parameters.Add("UserId", context.UserId);
            parameters.Add("MembershipId", context.MembershipId);
            parameters.Add("CanReadAll", context.CanReadAllOperationalRecords);
        }

        private static readonly IReadOnlyDictionary<string, ScopeMetadata> ScopeRegistry =
            new Dictionary<string, ScopeMetadata>(StringComparer.Ordinal)
            {
                ["AssessmentDto"] = new(
                    "tblAssessments", "AssessmentId", TenantRecordTypes.Assessment),
                ["BusinessDto"] = new(
                    "tblBusiness", "BusinessId", TenantRecordTypes.Business),
                ["ClientDto"] = new(
                    "tblClient", "ClientId", TenantRecordTypes.Client),
                ["CompanyDto"] = new(
                    "tblCompany", "CompanyId", TenantRecordTypes.Company),
                ["BranchDto"] = new(
                    "tblBranch", "BranchId", TenantRecordTypes.Branch)
            };

        private sealed record ScopeMetadata(
            string BackingTable,
            string KeyField,
            string EntityType);

        private static string GetValidatedField(string fieldName)
        {
            if (string.IsNullOrWhiteSpace(fieldName)
                || !QueryableFields.TryGetValue(fieldName, out var mappedField))
            {
                throw new ArgumentException(
                    $"'{fieldName}' is not a queryable field on {typeof(TDto).Name}.",
                    nameof(fieldName));
            }

            return mappedField;
        }

        private static string GetDefaultSortField()
        {
            var keyProperty = typeof(TDto).GetProperties()
                .FirstOrDefault(property =>
                    property.GetCustomAttributes().Any(attribute =>
                        attribute.GetType().Name is "KeyAttribute" or "ExplicitKeyAttribute"))
                ?? typeof(TDto).GetProperties()
                    .FirstOrDefault(property =>
                        property.Name.Equals($"{typeof(TDto).Name}Id", StringComparison.OrdinalIgnoreCase))
                ?? typeof(TDto).GetProperties().FirstOrDefault();

            return keyProperty == null
                ? throw new InvalidOperationException(
                    $"{typeof(TDto).Name} does not expose a sortable public property.")
                : GetValidatedField(keyProperty.Name);
        }

        private static string QuoteMultipartIdentifier(string identifier)
        {
            var segments = identifier.Split('.', StringSplitOptions.RemoveEmptyEntries);
            if (segments.Length == 0 || segments.Any(segment => !SqlIdentifierPattern.IsMatch(segment)))
                throw new InvalidOperationException($"Invalid table or view identifier '{identifier}'.");

            return string.Join(".", segments.Select(segment => $"[{segment}]"));
        }

        private static string EscapeLike(string value) =>
            value.Replace("\\", "\\\\", StringComparison.Ordinal)
                .Replace("%", "\\%", StringComparison.Ordinal)
                .Replace("_", "\\_", StringComparison.Ordinal)
                .Replace("[", "\\[", StringComparison.Ordinal);

        private static bool IsQueryableType(Type type)
        {
            type = Nullable.GetUnderlyingType(type) ?? type;
            return type.IsPrimitive
                || type.IsEnum
                || type == typeof(string)
                || type == typeof(decimal)
                || type == typeof(DateTime)
                || type == typeof(DateTimeOffset)
                || type == typeof(Guid);
        }
    }
}
