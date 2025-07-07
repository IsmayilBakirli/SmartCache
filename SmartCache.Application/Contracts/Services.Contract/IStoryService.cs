using SmartCache.Application.Contracts.Services.Contract.Base;
using SmartCache.Application.DTOs.Story;

namespace SmartCache.Application.Contracts.Services.Contract
{
    public interface IStoryService:IBaseService<StoryGetDto,StoryCreateDto,StoryUpdateDto>
    {
    }
}
