using Microsoft.AspNetCore.Mvc;
using SmartCache.Application.Common.Constants;
using SmartCache.Application.Common.Enums;
using SmartCache.Application.Common.Response;
using SmartCache.Application.Contracts.Services.Contract;
using SmartCache.Application.DTOs.Category;
using SmartCache.Application.DTOs.Service;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System;

namespace SmartCache.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ServicesController : ControllerBase
    {
        private readonly IServiceManager _serviceManager;

        public ServicesController(IServiceManager serviceManager)
        {
            _serviceManager = serviceManager;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var (data, version) = await _serviceManager.ServiceService.GetAllAsync();
            return Ok(new ApiResponse<object>(ResponseCode.Success, ResponseMessages.ServicesRetrieved, new {Version=version, Items = data }));
        }



        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var data = await _serviceManager.ServiceService.GetByIdAsync(id);
            return Ok(new ApiResponse<ServiceGetDto>(ResponseCode.Success, ResponseMessages.StoryRetrieved, data));
        }


        [HttpGet("has-changed/{clientVersion}")]
        public async Task<IActionResult> HasChanged(int clientVersion)
        {
            await _serviceManager.ServiceService.CheckVersionAsync(clientVersion);
            return Ok(new ApiResponse<object>(ResponseCode.Success, ResponseMessages.ChangeDeceted));
        }

        [HttpPost]
        public async Task<IActionResult> Add(ServiceCreateDto dto)
        {
            await _serviceManager.ServiceService.CreateAsync(dto);
            return Created("", new ApiResponse<object>(ResponseCode.Created, ResponseMessages.ServiceCreated));
        }

        [HttpPut]
        public async Task<IActionResult> Update(ServiceUpdateDto dto)
        {
            await _serviceManager.ServiceService.UpdateAsync(dto);
            return Ok(new ApiResponse<object>(ResponseCode.Updated, ResponseMessages.ServiceUpdated));
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            await _serviceManager.ServiceService.DeleteAsync(id);
            return Ok(new ApiResponse<object>(ResponseCode.Deleted, ResponseMessages.ServiceDeleted));
        }
    }
}
