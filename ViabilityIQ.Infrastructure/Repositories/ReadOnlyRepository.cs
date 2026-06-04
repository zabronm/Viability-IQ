using Dapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using ViabilityIQ.Application.Interfaces;
using ViabilityIQ.Infrastructure.DbFactory;
using ViabilityIQ.Shared.SharedModels;

namespace ViabilityIQ.Infrastructure.Repositories
{
    public class ReadOnlyRepository<TDto, TId> : IReadOnlyRepository<TDto, TId> where TDto : class
    {

        private readonly IDbConnectionFactory _dbConnectionFactory;
        private readonly string _tableName;

        public ReadOnlyRepository(IDbConnectionFactory dbConnectionFactory)
        {

            try
            {
                _dbConnectionFactory = dbConnectionFactory;
                               
                var tableAttr = typeof(TDto).GetCustomAttribute<TableNameAttribute>();
                if (tableAttr == null)
                {
                    throw new InvalidOperationException($"Architectural Error: The DTO type '{typeof(TDto).Name}' must be decorated with a [TableName] attribute.");
                }
                _tableName = tableAttr.Name;
            }
            catch (Exception ex)
            {

                throw;
            }
           
        }

        public async Task<IEnumerable<TDto>> GetAllAsync()
        {
            try
            {
                using var connection = _dbConnectionFactory.CreateConnection();
                string sql = $"SELECT * FROM {_tableName};";
                return await connection.QueryAsync<TDto>(sql);
            }
            catch (Exception ex)
            {
                throw;
            }
            
        }

        public async Task<IEnumerable<TDto>> GetListByIdAsync(string idFieldName, TId idValue)
        {
            try
            {
                using var connection = _dbConnectionFactory.CreateConnection();

                // Clean the parameter field input to safeguard the structural query string layout
                string cleanIdField = idFieldName.Replace("[", "").Replace("]", "");
                string sql = $"SELECT * FROM {_tableName} WHERE [{cleanIdField}] = @Id;";

                return await connection.QueryAsync<TDto>(sql, new { Id = idValue });
            }
            catch (Exception ex)
            {
                throw;
            }            
        }


        public async Task<TDto?> GetFirstOrDefaultAsync(string idFieldName, TId idValue)
        {
            try
            {
                using var connection = _dbConnectionFactory.CreateConnection();

                string cleanIdField = idFieldName.Replace("[", "").Replace("]", "");
                string sql = $"SELECT TOP 1 * FROM {_tableName} WHERE [{cleanIdField}] = @Id;";

                return await connection.QueryFirstOrDefaultAsync<TDto>(sql, new { Id = idValue });
            }
            catch (Exception ex)
            {
                throw;
            }
            
        }
    }
}
