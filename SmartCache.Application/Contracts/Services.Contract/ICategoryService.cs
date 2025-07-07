using SmartCache.Application.Contracts.Services.Contract.Base;
using SmartCache.Application.DTOs.Category;

namespace SmartCache.Application.Contracts.Services.Contract
{
    public interface ICategoryService:IBaseService<CategoryGetDto,CategoryCreateDto,CategoryUpdateDto>
    {
    }
}
