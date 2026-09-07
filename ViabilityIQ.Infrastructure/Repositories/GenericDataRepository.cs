using Dapper;
using Dapper.Contrib.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using ViabilityIQ.Application.Interfaces;
using ViabilityIQ.Infrastructure.DbFactory;
using ViabilityIQ.Shared.DataModelsInterfaces;

namespace ViabilityIQ.Infrastructure.Repositories
{
    public class GenericDataRepository<T> : IGenericDataRepository<T> where T : class
    {
        #region Private Fields
        private readonly IDbConnectionFactory _dbConnectionFactory;
        private readonly ISessionService _sessionService;
        #endregion

        #region Constructor
        public GenericDataRepository(
            IDbConnectionFactory dbConnectionFactory,
            ISessionService sessionService)
        {
            _dbConnectionFactory = dbConnectionFactory ?? throw new ArgumentNullException(nameof(dbConnectionFactory));
            _sessionService = sessionService ?? throw new ArgumentNullException(nameof(sessionService));
        }
        #endregion

        #region READ OPERATIONS

        
        /// Gets a single entity by its ID        
        public async Task<T?> GetByIdAsync(long id)
        {
            try
            {
                using var connection = _dbConnectionFactory.CreateConnection();
                return await connection.GetAsync<T>(id);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Database Get By ID Error: {ex.Message}");
                throw;
            }
        }

        
        /// Gets all entities without filtering        
        public async Task<IEnumerable<T>> GetAllAsync()
        {
            try
            {
                using var connection = _dbConnectionFactory.CreateConnection();
                var items = await connection.GetAllAsync<T>();

                // If the entity implements ISortableEntity, sort it dynamically by its DisplayName
                if (typeof(ISortableEntity).IsAssignableFrom(typeof(T)))
                {
                    return items.Cast<ISortableEntity>()
                                .OrderBy(x => x.DisplayName)
                                .Cast<T>()
                                .ToList();
                }

                return items.ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Database Generic Read Error: {ex.Message}");
                throw;
            }
        }

        
        /// Gets all entities matching a predicate filter
        /// Note: This performs client-side filtering since Dapper.Contrib doesn't support LINQ        
        public async Task<IEnumerable<T>> GetAllAsync(Expression<Func<T, bool>> predicate)
        {
            try
            {
                using var connection = _dbConnectionFactory.CreateConnection();
                var allItems = await connection.GetAllAsync<T>();

                // Compile the predicate and apply it client-side
                var compiled = predicate.Compile();
                var filteredItems = allItems.Where(compiled).ToList();

                // If the entity implements ISortableEntity, sort it
                if (typeof(ISortableEntity).IsAssignableFrom(typeof(T)))
                {
                    return filteredItems.Cast<ISortableEntity>()
                                        .OrderBy(x => x.DisplayName)
                                        .Cast<T>()
                                        .ToList();
                }

                return filteredItems;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Database Generic Filter Error: {ex.Message}");
                throw;
            }
        }

        
        /// Gets entities filtered by a specific field value
        /// More efficient for simple single-field filtering than GetAllAsync(predicate)
        /// Example: GetByFieldAsync("AssessmentId", 123)        
        public async Task<IEnumerable<T>> GetByFieldAsync(string fieldName, object fieldValue)
        {
            try
            {
                using var connection = _dbConnectionFactory.CreateConnection();

                // Validate field name to prevent SQL injection
                if (string.IsNullOrWhiteSpace(fieldName))
                    throw new ArgumentException("Field name cannot be empty", nameof(fieldName));

                // Build dynamic WHERE clause
                var sql = $"SELECT * FROM [{typeof(T).Name}] WHERE [{fieldName}] = @Value";

                var result = await connection.QueryAsync<T>(sql, new { Value = fieldValue });

                // Apply sorting if applicable
                if (typeof(ISortableEntity).IsAssignableFrom(typeof(T)))
                {
                    return result.Cast<ISortableEntity>()
                                 .OrderBy(x => x.DisplayName)
                                 .Cast<T>()
                                 .ToList();
                }

                return result.ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Database Get By Field Error: {ex.Message}");
                throw;
            }
        }

        
        /// Gets entities filtered by multiple field values
        /// Example: GetByFieldsAsync(new { AssessmentId = 123, IsActive = true })        
        public async Task<IEnumerable<T>> GetByFieldsAsync(object filters)
        {
            try
            {
                if (filters == null)
                    throw new ArgumentNullException(nameof(filters));

                using var connection = _dbConnectionFactory.CreateConnection();

                // Build WHERE clause from anonymous object properties
                var properties = filters.GetType().GetProperties();
                if (properties.Length == 0)
                    throw new ArgumentException("Filters object must have at least one property", nameof(filters));

                var whereClause = string.Join(" AND ",
                    properties.Select(p => $"[{p.Name}] = @{p.Name}"));

                var sql = $"SELECT * FROM [{typeof(T).Name}] WHERE {whereClause}";

                var result = await connection.QueryAsync<T>(sql, filters);

                // Apply sorting if applicable
                if (typeof(ISortableEntity).IsAssignableFrom(typeof(T)))
                {
                    return result.Cast<ISortableEntity>()
                                 .OrderBy(x => x.DisplayName)
                                 .Cast<T>()
                                 .ToList();
                }

                return result.ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Database Get By Fields Error: {ex.Message}");
                throw;
            }
        }

        #endregion

        #region WRITE OPERATIONS

        
        /// Saves (inserts or updates) an entity
        /// Automatically handles audit trail properties        
        public async Task<bool> SaveAsync(T entity)
        {
            using var connection = _dbConnectionFactory.CreateConnection();

            try
            {
                // Handle audit trails generically using our interface hook
                if (entity is IAuditableEntity auditable)
                {
                    if (entity is IEntity identity && identity.Id == 0)
                    {
                        // INSERT
                        auditable.CreatedDate = DateTime.UtcNow;
                        auditable.CreatedBy = _sessionService.UserId;
                        auditable.Active = true;

                        var newId = await connection.InsertAsync(entity);
                        return newId > 0;
                    }
                    else
                    {
                        // UPDATE
                        auditable.ModifiedDate = DateTime.UtcNow;
                        auditable.ModifiedBy = _sessionService.UserId;

                        return await connection.UpdateAsync(entity);
                    }
                }

                // Fallback for models that do not have audit features
                return await connection.UpdateAsync(entity);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Database Save Error: {ex.Message}");
                throw;
            }
        }

        
        /// Deletes an entity
        
        public async Task<bool> DeleteAsync(T entity)
        {
            try
            {
                using var connection = _dbConnectionFactory.CreateConnection();
                return await connection.DeleteAsync(entity);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Database Delete Error: {ex.Message}");
                throw;
            }
        }

        #endregion

        #region UTILITY OPERATIONS

        
        /// Counts entities matching a predicate filter        
        public async Task<int> CountAsync(Expression<Func<T, bool>> predicate)
        {
            try
            {
                using var connection = _dbConnectionFactory.CreateConnection();
                var allItems = await connection.GetAllAsync<T>();

                // Compile the predicate and count matching items
                var compiled = predicate.Compile();
                return allItems.Count(compiled);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Database Count Error: {ex.Message}");
                throw;
            }
        }

        
        /// Counts all entities in the table        
        public async Task<int> CountAsync()
        {
            try
            {
                using var connection = _dbConnectionFactory.CreateConnection();
                var allItems = await connection.GetAllAsync<T>();
                return allItems.Count();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Database Count Error: {ex.Message}");
                throw;
            }
        }

        
        /// Checks if any entity matches a predicate filter        
        public async Task<bool> AnyAsync(Expression<Func<T, bool>> predicate)
        {
            try
            {
                using var connection = _dbConnectionFactory.CreateConnection();
                var allItems = await connection.GetAllAsync<T>();

                // Compile the predicate and check if any item matches
                var compiled = predicate.Compile();
                return allItems.Any(compiled);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Database Any Error: {ex.Message}");
                throw;
            }
        }

        #endregion
    }
}
