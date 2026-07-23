using Dapper.Contrib.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViabilityIQ.Application.Interfaces;
using ViabilityIQ.Infrastructure.DbFactory;
using ViabilityIQ.Shared.DataModelsInterfaces;

namespace ViabilityIQ.Infrastructure.Repositories
{
    public class GenericDataRepository<T>: IGenericDataRepository<T> where T : class
    {
        private readonly IDbConnectionFactory _dbConnectionFactory;
        private readonly ISessionService _sessionService;

        public GenericDataRepository(IDbConnectionFactory dbConnectionFactory, ISessionService sessionService)
        {
            _dbConnectionFactory = dbConnectionFactory;
            _sessionService = sessionService;
        }


        public async Task<T?> GetByIdAsync(long id)
        {
            using var connection = _dbConnectionFactory.CreateConnection();
            return await connection.GetAsync<T>(id);
        }


        public async Task<IEnumerable<T>> GetAllAsync()
        {
            try
            {
                using var connection = _dbConnectionFactory.CreateConnection();
                var items = await connection.GetAllAsync<T>();

                // If the entity implements ISortableEntity, sort it dynamically by its DisplayName!
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
                // Log exception uniformly here
                Console.WriteLine($"Database Generic Read Error: {ex.Message}");
                return Enumerable.Empty<T>();
            }
        }

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
                        auditable.CreatedDate = DateTime.UtcNow;
                        auditable.CreatedBy = _sessionService.UserId;
                        auditable.Active = true;

                        var newId = await connection.InsertAsync(entity);
                        return newId > 0;
                    }
                    else
                    {
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
                throw;
            }
        }

        public async Task<bool> DeleteAsync(T entity)
        {
            using var connection = _dbConnectionFactory.CreateConnection();
            return await connection.DeleteAsync(entity);
        }
    }
}
