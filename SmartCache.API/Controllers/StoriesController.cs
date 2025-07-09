using Microsoft.AspNetCore.Mvc;
using SmartCache.Application.Common.Constants;
using SmartCache.Application.Common.Enums;
using SmartCache.Application.Common.Response;
using SmartCache.Application.Contracts.Services.Contract;
using SmartCache.Application.DTOs.Story;

namespace SmartCache.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StoriesController : ControllerBase
    {
        private readonly IServiceManager _serviceManager;
        public StoriesController(IServiceManager serviceManager)
        {
            _serviceManager = serviceManager;
        }


        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var (data, version) = await _serviceManager.StoryService.GetAllAsync();
            return Ok(new ApiResponse<object>(ResponseCode.Success, ResponseMessages.StoriesRetrieved, data,version));
        }


        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var data = await _serviceManager.StoryService.GetByIdAsync(id);
            return Ok(new ApiResponse<StoryGetDto>(ResponseCode.Success, ResponseMessages.StoryRetrieved, data));
        }


        [HttpPost]
        public async Task<IActionResult> Add(StoryCreateDto dto)
        {
            await _serviceManager.StoryService.CreateAsync(dto);
            return Created("", new ApiResponse<object>(ResponseCode.Created, ResponseMessages.StoryCreated));
        }


        [HttpPut]
        public async Task<IActionResult> Update(StoryUpdateDto dto)
        {
            await _serviceManager.StoryService.UpdateAsync(dto);
            return Ok(new ApiResponse<object>(ResponseCode.Updated, ResponseMessages.StoryUpdated));
        }


        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            await _serviceManager.StoryService.DeleteAsync(id);
            return Ok(new ApiResponse<object>(ResponseCode.Deleted, ResponseMessages.StoryDeleted));
        }
    }
}
