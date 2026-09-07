using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace ViabilityIQ.Application.Interfaces
{
    public interface IGenericDataRepository<T> where T : class
    {
        // ====================================================
        // READ OPERATIONS
        // ====================================================

        
        /// Gets a single entity by its ID        
        /// <param name="id">The entity ID</param>
        /// <returns>The entity or null if not found</returns>
        Task<T?> GetByIdAsync(long id);

        
        /// Gets all entities without filtering        
        /// <returns>All entities from the table</returns>
        Task<IEnumerable<T>> GetAllAsync();

        
        /// Gets entities matching a predicate filter (LINQ expression)
        /// Note: This performs client-side filtering        
        /// <param name="predicate">The filter predicate</param>
        /// <returns>Filtered entities</returns>
        Task<IEnumerable<T>> GetAllAsync(Expression<Func<T, bool>> predicate);

        
        /// Gets entities filtered by a specific field value
        /// This is more efficient for simple single-field filtering
        /// Example: GetByFieldAsync("AssessmentId", 123)
        
        /// <param name="fieldName">The property name to filter by</param>
        /// <param name="fieldValue">The value to match</param>
        /// <returns>Entities matching the field value</returns>
        Task<IEnumerable<T>> GetByFieldAsync(string fieldName, object fieldValue);

        
        /// Gets entities filtered by multiple field values
        /// Example: GetByFieldsAsync(new { AssessmentId = 123, IsActive = true })        
        /// <param name="filters">Anonymous object with field names and values</param>
        /// <returns>Entities matching all filter conditions</returns>
        Task<IEnumerable<T>> GetByFieldsAsync(object filters);

        // ====================================================
        // WRITE OPERATIONS
        // ====================================================

        
        /// Saves (inserts or updates) an entity
        /// Automatically handles audit trail properties
        
        /// <param name="entity">The entity to save</param>
        /// <returns>True if save was successful</returns>
        Task<bool> SaveAsync(T entity);

        
        /// Deletes an entity        
        /// <param name="entity">The entity to delete</param>
        /// <returns>True if delete was successful</returns>
        Task<bool> DeleteAsync(T entity);

        // ====================================================
        // UTILITY OPERATIONS
        // ====================================================
        
        /// Counts entities matching a predicate filter        
        /// <param name="predicate">The filter predicate</param>
        /// <returns>Count of matching entities</returns>
        Task<int> CountAsync(Expression<Func<T, bool>> predicate);

        
        /// Counts all entities in the table        
        /// <returns>Total count of entities</returns>
        Task<int> CountAsync();

        
        /// Checks if any entity matches a predicate filter        
        /// <param name="predicate">The filter predicate</param>
        /// <returns>True if at least one entity matches</returns>
        Task<bool> AnyAsync(Expression<Func<T, bool>> predicate);
    }
}
