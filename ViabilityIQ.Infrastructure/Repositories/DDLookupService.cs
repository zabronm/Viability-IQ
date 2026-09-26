using Dapper;
using ViabilityIQ.Application.Interfaces;
using ViabilityIQ.Infrastructure.DbFactory;
using ViabilityIQ.Shared.DataModels.SecurityDataModels;
using ViabilityIQ.Shared.SharedModels;

namespace ViabilityIQ.Infrastructure.Repositories;

public sealed class DDLookupService : IDDLookupService
{
    private readonly IDbConnectionFactory _dbConnectionFactory;
    private readonly ITenantAuthorizationService _tenantAuthorizationService;
    private readonly ISessionService _sessionService;

    private static readonly IReadOnlyDictionary<DDLookupEnums, LookupMetadata>
        MetadataRegistry = new Dictionary<DDLookupEnums, LookupMetadata>
        {
            [DDLookupEnums.AssessmentTypes] = Global(
                "tblAssessmentType", "AssessmentTypeId", "AssessmentTypeName"),
            [DDLookupEnums.Genders] = Global("tblGender", "GenderId", "Gender"),
            [DDLookupEnums.Races] = Global("tblRace", "RaceId", "Race"),
            [DDLookupEnums.Banks] = Global("tblBank", "BankId", "BankName"),
            [DDLookupEnums.LoanTypes] = Global(
                "tblLoanType", "LoanTypeId", "LoanTypeName"),
            [DDLookupEnums.BusinessCategories] = Global(
                "tblBusinessCategories", "BusinessCategoryId", "BusinessCategoryName"),
            [DDLookupEnums.AssetCategories] = Global(
                "tblAssetCategory", "AssetCategoryId", "CategoryName"),
            [DDLookupEnums.AssetTypes] = Global(
                "tblAssetType", "AssetTypeId", "TypeName"),
            [DDLookupEnums.ClientCategories] = Global(
                "tblClientCategories", "ClientCategoryId", "ClientCategoryName"),
            [DDLookupEnums.ProductServiceCategories] = Global(
                "tblProductCategory", "ProductCategoryId", "ProductCategoryName"),
            [DDLookupEnums.Products] = Global(
                "tblProduct", "ProductServiceId", "ProductServiceName"),
            [DDLookupEnums.Provinces] = Global(
                "tblProvince", "ProvinceId", "ProvinceName"),
            [DDLookupEnums.Sectors] = Global(
                "tblBusinessSector", "BusinessSectorId", "BusinessSectorName"),
            [DDLookupEnums.ClientTypes] = Global(
                "tblClientType", "ClientTypeId", "ClientTypeName"),
            [DDLookupEnums.IncomeTypes] = Global(
                "tblIncomeType", "IncomeTypeId", "IncomeTypeName"),
            [DDLookupEnums.ExpenseTypes] = Global(
                "tblExpenseType", "ExpenseTypeId", "ExpenseTypeName"),
            [DDLookupEnums.ExpenseItems] = Global(
                "tblExpenseItems", "ExpenseItemId", "ExpenseItemName"),

            [DDLookupEnums.Businesses] = Tenant(
                "tblBusiness", "BusinessId", "BusinessName",
                TenantRecordTypes.Business),
            [DDLookupEnums.Clients] = Tenant(
                "tblClient", "ClientId", "FullName",
                TenantRecordTypes.Client),
            [DDLookupEnums.Assessments] = Tenant(
                "tblAssessments", "AssessmentId", "CaseNumber",
                TenantRecordTypes.Assessment),
            [DDLookupEnums.Company] = Tenant(
                "tblCompany", "CompanyId", "CompanyName",
                TenantRecordTypes.Company),

            [DDLookupEnums.AssessmentSalesCategories] = AssessmentChild(
                "tblAssessmentSalesCategory",
                "AssessmentSalesCategoryId",
                "AssessmentSalesCategoryName"),
            [DDLookupEnums.AssessmentLoans] = AssessmentChild(
                "vw_assessment_loans",
                "AssessmentLoanId",
                "LoanDescription")
        };

    public DDLookupService(
        IDbConnectionFactory dbConnectionFactory,
        ITenantAuthorizationService tenantAuthorizationService,
        ISessionService sessionService)
    {
        _dbConnectionFactory = dbConnectionFactory;
        _tenantAuthorizationService = tenantAuthorizationService;
        _sessionService = sessionService;
    }

    public async Task<IEnumerable<LookupItem>> GetLookupOptionsAsync(
        DDLookupEnums lookupKey,
        string? filterField = null,
        object? filterValue = null)
    {
        if (lookupKey == DDLookupEnums.Users)
        {
            return await GetTenantUsersAsync();
        }

        if (!MetadataRegistry.TryGetValue(lookupKey, out var metadata))
        {
            throw new ArgumentException(
                $"No trusted lookup metadata is configured for {lookupKey}.",
                nameof(lookupKey));
        }

        var parameters = new DynamicParameters();
        var predicates = new List<string> { "sourceRecord.[Active] = 1" };

        if (!string.IsNullOrWhiteSpace(filterField) || filterValue is not null)
        {
            var effectiveFilterField = string.IsNullOrWhiteSpace(filterField)
                ? metadata.ParentIdField
                : filterField;
            if (!string.Equals(
                    effectiveFilterField,
                    metadata.ParentIdField,
                    StringComparison.OrdinalIgnoreCase)
                || metadata.ParentIdField is null
                || filterValue is null)
            {
                throw new ArgumentException(
                    $"The requested filter is not supported for {lookupKey}.",
                    nameof(filterField));
            }

            predicates.Add(
                $"sourceRecord.[{metadata.ParentIdField}] = @ParentId");
            parameters.Add("ParentId", filterValue);
        }

        if (metadata.Scope != LookupScope.Global)
        {
            await _tenantAuthorizationService.EnsureCanReadOperationalDataAsync();
            var context = await _tenantAuthorizationService.GetAccessContextAsync();
            AddScopeParameters(parameters, context);

            if (metadata.Scope == LookupScope.DirectTenant)
            {
                predicates.Add(BuildDirectTenantPredicate(metadata));
            }
            else
            {
                var assessmentId = GetAssessmentId(filterValue);
                parameters.Add("AssessmentId", assessmentId);
                predicates.Add("sourceRecord.[AssessmentId] = @AssessmentId");
                predicates.Add(BuildAssessmentPredicate());
            }
        }

        var query = $"""
            SELECT
                sourceRecord.[{metadata.IdField}] AS Id,
                sourceRecord.[{metadata.DisplayField}] AS Description
            FROM [{metadata.Table}] sourceRecord
            WHERE {string.Join(" AND ", predicates)}
            ORDER BY sourceRecord.[{metadata.DisplayField}] ASC;
            """;

        using var connection = _dbConnectionFactory.CreateConnection();
        return (await connection.QueryAsync<LookupItem>(query, parameters)).AsList();
    }

    private async Task<IEnumerable<LookupItem>> GetTenantUsersAsync()
    {
        await _tenantAuthorizationService.EnsureCanReadOperationalDataAsync();
        var context = await _tenantAuthorizationService.GetAccessContextAsync();
        const string query = """
            SELECT
                users.Id,
                LTRIM(RTRIM(CONCAT(users.FirstName, ' ', users.LastName))) AS Description
            FROM tblTenantMembership membership
            INNER JOIN tblApplicationUsers users
                ON users.Id = membership.UserId
               AND users.IsActive = 1
            WHERE membership.TenantId = @TenantId
              AND membership.Active = 1
              AND membership.MembershipStatus = 'Active'
            ORDER BY users.FirstName, users.LastName;
            """;

        using var connection = _dbConnectionFactory.CreateConnection();
        return (await connection.QueryAsync<LookupItem>(
            query,
            new { context.TenantId })).AsList();
    }

    private long GetAssessmentId(object? filterValue)
    {
        var assessmentId = filterValue switch
        {
            long value => value,
            int value => value,
            _ => _sessionService.AssessmentId ?? 0
        };

        if (assessmentId <= 0)
        {
            throw new InvalidOperationException(
                "An active assessment is required for this lookup.");
        }

        return assessmentId;
    }

    private static string BuildDirectTenantPredicate(LookupMetadata metadata) => $"""
        sourceRecord.TenantId = @TenantId
        AND
        (
            @CanReadAll = 1
            OR sourceRecord.CreatedBy = @UserId
            OR EXISTS
            (
                SELECT 1
                FROM tblTenantRecordGrant grantRecord
                WHERE grantRecord.TenantId = @TenantId
                  AND grantRecord.TenantMembershipId = @MembershipId
                  AND grantRecord.EntityType = '{metadata.EntityType}'
                  AND grantRecord.EntityId = sourceRecord.[{metadata.IdField}]
                  AND grantRecord.Active = 1
                  AND grantRecord.CanView = 1
            )
        )
        """;

    private static string BuildAssessmentPredicate() => """
        EXISTS
        (
            SELECT 1
            FROM tblAssessments assessment
            WHERE assessment.AssessmentId = sourceRecord.AssessmentId
              AND assessment.TenantId = @TenantId
              AND
              (
                  @CanReadAll = 1
                  OR assessment.CreatedBy = @UserId
                  OR EXISTS
                  (
                      SELECT 1
                      FROM tblTenantRecordGrant grantRecord
                      WHERE grantRecord.TenantId = @TenantId
                        AND grantRecord.TenantMembershipId = @MembershipId
                        AND grantRecord.EntityType = 'Assessment'
                        AND grantRecord.EntityId = assessment.AssessmentId
                        AND grantRecord.Active = 1
                        AND grantRecord.CanView = 1
                  )
              )
        )
        """;

    private static void AddScopeParameters(
        DynamicParameters parameters,
        TenantAccessContext context)
    {
        parameters.Add("TenantId", context.TenantId);
        parameters.Add("UserId", context.UserId);
        parameters.Add("MembershipId", context.MembershipId);
        parameters.Add("CanReadAll", context.CanReadAllOperationalRecords);
    }

    private static LookupMetadata Global(
        string table,
        string idField,
        string displayField) =>
        new(table, idField, displayField, null, LookupScope.Global, null);

    private static LookupMetadata Tenant(
        string table,
        string idField,
        string displayField,
        string entityType) =>
        new(table, idField, displayField, null, LookupScope.DirectTenant, entityType);

    private static LookupMetadata AssessmentChild(
        string table,
        string idField,
        string displayField) =>
        new(
            table,
            idField,
            displayField,
            "AssessmentId",
            LookupScope.AssessmentChild,
            TenantRecordTypes.Assessment);

    private sealed record LookupMetadata(
        string Table,
        string IdField,
        string DisplayField,
        string? ParentIdField,
        LookupScope Scope,
        string? EntityType);

    private enum LookupScope
    {
        Global,
        DirectTenant,
        AssessmentChild
    }
}
