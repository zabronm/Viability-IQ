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
        private readonly Dictionary<DDLookupEnums, (string Table, string IdField, string DisplayField)> _metadataRegistry = new()
        {
            { DDLookupEnums.Banks, ("tblBank", "BankId", "BankName") },
            { DDLookupEnums.LoanTypes, ("tblLoanTypes", "LoanTypeId", "LoanTypeName") },
            { DDLookupEnums.BusinessCategories, ("tblBusinessCategories", "BusinessCategoryId", "BusinessCategoryName") },
            { DDLookupEnums.Businesses, ("tblBusiness", "BusinessId", "BusinessName") },
            { DDLookupEnums.ClientCategories, ("tblClientCategories", "ClientCategoryId", "ClientCategoryName") },
            { DDLookupEnums.ProductServiceCategories, ("tblProductCategory", "ProductCategoryId", "ProductCategoryName") },
            { DDLookupEnums.Products, ("tblProduct", "ProductServiceId", "ProductServiceName") },
            { DDLookupEnums.Provinces, ("tblProvince", "ProvinceId", "ProvinceName") },
            { DDLookupEnums.Users, ("tblUsers", "UserId", "FullName") },
            { DDLookupEnums.Sectors, ("tblBusinessSector", "BusinessSectorId", "BusinessSector") },
            { DDLookupEnums.Clients, ("tblClient", "ClientId", "FullName") },
        };

        public DDLookupService(IDbConnectionFactory dbConnectionFactory, IMemoryCache cache)
        {
            _dbConnectionFactory = dbConnectionFactory;
            _cache = cache;
        }

        public async Task<IEnumerable<LookupItem>> GetLookupOptionsAsync(DDLookupEnums lookupKey)
        {
            // Safeguard against missing registry settings keys
            if (!_metadataRegistry.TryGetValue(lookupKey, out var meta))
            {
                throw new ArgumentException($"Configuration Missing: No table metadata mapped for LookupKey: {lookupKey}");
            }

            string cacheKey = $"lookup_key_{lookupKey}".ToLower();

            if (!_cache.TryGetValue(cacheKey, out IEnumerable<LookupItem>? cachedItems))
            {
                // Construct query using trusted internal strings (eliminates SQL Injection completely)
                string query = $@"SELECT [{meta.IdField}] AS Id, [{meta.DisplayField}] AS Description 
                                  FROM {meta.Table} 
                                  ORDER BY [{meta.DisplayField}] ASC;";

                using var connection = _dbConnectionFactory.CreateConnection();
                cachedItems = await connection.QueryAsync<LookupItem>(query);

                var cacheOptions = new MemoryCacheEntryOptions().SetAbsoluteExpiration(CacheDuration);
                _cache.Set(cacheKey, cachedItems, cacheOptions);
            }

            return cachedItems ?? Array.Empty<LookupItem>();
        }
    }
}
