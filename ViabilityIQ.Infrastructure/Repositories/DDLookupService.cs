using Dapper;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ViabilityIQ.Application.Interfaces;
using ViabilityIQ.Infrastructure.DbFactory;
using ViabilityIQ.Shared.SharedModels;


namespace ViabilityIQ.Infrastructure.Repositories
{
    public class DDLookupService : IDDLookupService
    {
        private readonly IDbConnectionFactory _dbConnectionFactory;
        private readonly IMemoryCache _cache;
        private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(10);

        // 1. Centralized hardcoded database mapping definitions mapping
        //private readonly Dictionary<DDLookupEnums, (string Table, string IdField, string DisplayField)> _metadataRegistry = new()
        private readonly Dictionary<DDLookupEnums, (string Table, string IdField, string DisplayField, string? ParentIdField)> _metadataRegistry = new()
        {
            //============= master data lookups  ======================================
            { DDLookupEnums.AssessmentTypes, ("tblAssessmentType", "AssessmentTypeId", "AssessmentTypeName", null) },
            { DDLookupEnums.Genders, ("tblGender", "GenderId", "Gender", null) },
            { DDLookupEnums.Races, ("tblRace", "RaceId", "Race", null) },
            { DDLookupEnums.Banks, ("tblBank", "BankId", "BankName", null) },
            { DDLookupEnums.LoanTypes, ("tblLoanType", "LoanTypeId", "LoanTypeName", null) },
            { DDLookupEnums.BusinessCategories, ("tblBusinessCategories", "BusinessCategoryId", "BusinessCategoryName", null) },
            { DDLookupEnums.Businesses, ("tblBusiness", "BusinessId", "BusinessName", null) },
            { DDLookupEnums.ClientCategories, ("tblClientCategories", "ClientCategoryId", "ClientCategoryName", null) },
            { DDLookupEnums.ProductServiceCategories, ("tblProductCategory", "ProductCategoryId", "ProductCategoryName", null) },
            { DDLookupEnums.Products, ("tblProduct", "ProductServiceId", "ProductServiceName", null) },
            { DDLookupEnums.Provinces, ("tblProvince", "ProvinceId", "ProvinceName", null) },
            { DDLookupEnums.Users, ("tblUsers", "UserId", "FullName", null) },
            { DDLookupEnums.Sectors, ("tblBusinessSector", "BusinessSectorId", "BusinessSectorName", null) },
            { DDLookupEnums.Clients, ("tblClient", "ClientId", "FullName", null) },
            { DDLookupEnums.ClientTypes, ("tblClientType", "ClientTypeId", "ClientTypeName", null) },
            { DDLookupEnums.IncomeTypes, ("tblIncomeType", "IncomeTypeId", "IncomeTypeName", null) },
            { DDLookupEnums.ExpenseTypes, ("tblExpenseType", "ExpenseTypeId", "ExpenseTypeName", null) },
            { DDLookupEnums.ExpenseItems, ("tblExpenseItems", "ExpenseItemId", "ExpenseItemName", null) },


            //============= assessment lookups =============================================================
            //============= Usage =>(Table/View, IdField(bound field), DisplayField, ParentIdField(parameter field)):)  =======================
            { DDLookupEnums.AssessmentSalesCategories, ("tblAssessmentSalesCategory", "AssessmentSalesCategoryId", "AssessmentSalesCategoryName", null) },
            { DDLookupEnums.AssessmentLoans, ("vw_assessment_loans", "AssessmentLoanId", "LoanDescription", "AssessmentId") },
        };


        public DDLookupService(IDbConnectionFactory dbConnectionFactory, IMemoryCache cache)
        {
            _dbConnectionFactory = dbConnectionFactory;
            _cache = cache;
        }


        public async Task<IEnumerable<LookupItem>> GetLookupOptionsAsync(DDLookupEnums lookupKey,
                                                                         string? filterField = null,
                                                                         object? filterValue = null)
        {
            // Safeguard against missing registry settings keys
            if (!_metadataRegistry.TryGetValue(lookupKey, out var meta))
            {
                throw new ArgumentException($"Configuration Missing: No table metadata mapped for LookupKey: {lookupKey}");
            }

            // Build unique cache key including field and value
            string cacheKey = $"lookup_{lookupKey}" +
                (!string.IsNullOrWhiteSpace(filterField) && filterValue != null ? $"_{filterField}_{filterValue}" : "").ToLower();

            if (!_cache.TryGetValue(cacheKey, out IEnumerable<LookupItem>? cachedItems))
            {
                using var connection = _dbConnectionFactory.CreateConnection();

                string query = $@"SELECT [{meta.IdField}] AS Id, [{meta.DisplayField}] AS Description 
                          FROM {meta.Table}";

                var parameters = new DynamicParameters();

                // Apply dynamic WHERE clause if field and value are provided
                if (!string.IsNullOrWhiteSpace(filterField) && filterValue != null)
                {
                    // Sanitize field name to avoid unsafe column identifiers
                    string safeFieldName = Regex.Replace(filterField, @"[^\w]", "");
                    query += $" WHERE [{safeFieldName}] = @FilterValue";
                    parameters.Add("FilterValue", filterValue);
                }

                query += $" ORDER BY [{meta.DisplayField}] ASC;";

                cachedItems = await connection.QueryAsync<LookupItem>(query, parameters);

                var cacheOptions = new MemoryCacheEntryOptions().SetAbsoluteExpiration(CacheDuration);
                _cache.Set(cacheKey, cachedItems, cacheOptions);
            }

            return cachedItems ?? Array.Empty<LookupItem>();
        }
    }
}
