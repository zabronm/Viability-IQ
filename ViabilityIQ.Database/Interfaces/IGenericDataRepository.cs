using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ViabilityIQ.Application.Interfaces
{
    public interface IGenericDataRepository<T> where T : class
    {
        Task<T?> GetByIdAsync(long id);
        Task<IEnumerable<T>> GetAllAsync();
        Task<bool> SaveAsync(T entity);
        Task<bool> DeleteAsync(T entity);
    }
}
