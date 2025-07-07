using SmartCache.Application.Contracts.Services.Contract.Base;
using SmartCache.Application.DTOs.Service;

namespace SmartCache.Application.Contracts.Services.Contract
{
    public interface IServiceService:IBaseService<ServiceGetDto,ServiceCreateDto,ServiceUpdateDto>
    {
    }
}
