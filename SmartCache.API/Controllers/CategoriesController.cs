using Microsoft.AspNetCore.Mvc;
using SmartCache.Application.Common.Constants;
using SmartCache.Application.Common.Enums;
using SmartCache.Application.Common.Response;
using SmartCache.Application.Contracts.Services.Contract;
using SmartCache.Application.DTOs.Category;

namespace SmartCache.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CategoriesController : ControllerBase
    {
        private readonly IServiceManager _serviceManager;
        public CategoriesController(IServiceManager serviceManager)
        {
            _serviceManager = serviceManager;
        }


        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var (data, version) = await _serviceManager.CategoryService.GetAllAsync();
            return Ok(new ApiResponse<object>(ResponseCode.Success, ResponseMessages.ServicesRetrieved, new {Version=version,Items=data}));

        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var data = await _serviceManager.CategoryService.GetByIdAsync(id);
            return Ok(new ApiResponse<CategoryGetDto>(ResponseCode.Success, ResponseMessages.StoryRetrieved, data));
        }


        [HttpGet("has-changed/{clientVersion}")]
        public async Task<IActionResult> HasChanged(int clientVersion)
        {
            await _serviceManager.CategoryService.CheckVersionAsync(clientVersion);
            return Ok(new ApiResponse<object>(ResponseCode.Success, ResponseMessages.ChangeDeceted));
        }
        [HttpPost]
        public async Task<IActionResult> Add(CategoryCreateDto dto)
        {
            await _serviceManager.CategoryService.CreateAsync(dto);
            return Created("", new ApiResponse<object>(ResponseCode.Created, ResponseMessages.CategoryCreated));
        }


        [HttpPut]
        public async Task<IActionResult> Update(CategoryUpdateDto dto)
        {
            await _serviceManager.CategoryService.UpdateAsync(dto);
            return Ok(new ApiResponse<object>(ResponseCode.Updated,ResponseMessages.CategoryUpdated));
        }


        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            await _serviceManager.CategoryService.DeleteAsync(id);
            return Ok(new ApiResponse<object>(ResponseCode.Deleted,ResponseMessages.CategoryDeleted));
        }
    }
}
