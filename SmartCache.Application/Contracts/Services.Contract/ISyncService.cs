using SmartCache.Application.DTOs.Version;

namespace SmartCache.Application.Contracts.Services.Contract
{
    public interface ISyncService
    {
        Task<List<VersionResponseDto>> CheckVersions(VersionCheckRequestDto dto);
        
    }
}
