using SmartCache.Domain.Entities.Common;

namespace SmartCache.Application.Contracts.Repositories.Contract.Base
{
    public interface IBaseRepository<T> where T : BaseEntity
    {
        Task<List<T>> GetAllAsync(int skip = 0, int take = int.MaxValue);
        Task<T> FindByIdAsync(int id);
        Task<T> CreateAsync(T entity);
        Task UpdateAsync(T entity);
        Task DeleteAsync(T entity);
    }
}
