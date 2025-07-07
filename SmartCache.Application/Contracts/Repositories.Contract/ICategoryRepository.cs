using SmartCache.Application.Contracts.Repositories.Contract.Base;
using SmartCache.Domain.Entities;

namespace SmartCache.Application.Contracts.Repositories.Contract
{
    public interface ICategoryRepository:IBaseRepository<Category>
    {
        Task<bool> CanDeleteCategoryAsync(int categoryId);
    }
}