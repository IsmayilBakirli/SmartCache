using Microsoft.AspNetCore.Mvc;
using SmartCache.Application.Common.Constants;
using SmartCache.Application.Common.Enums;
using SmartCache.Application.Common.Response;
using SmartCache.Application.Contracts.Services.Contract;
using SmartCache.Application.DTOs.Version;

namespace SmartCache.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SyncController : ControllerBase
    {
        private readonly IServiceManager _serviceManager;
        public SyncController(IServiceManager serviceManager)
        {
            _serviceManager = serviceManager;
        }
        [HttpPost("check-versions")]
        public async Task<IActionResult> CheckVersions(VersionCheckRequestDto dto)
        {
            var response=await _serviceManager.SyncService.CheckVersions(dto);
            return Ok(new ApiResponse<object>(ResponseCode.Success,ResponseMessages.SyncSuccessFull,response));
        }
    }
}
