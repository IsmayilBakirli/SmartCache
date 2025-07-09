using SmartCache.Application.Contracts.Services.Contract;
using SmartCache.Application.DTOs.Version;

namespace SmartCache.Persistence.Services
{
    public class SyncService : ISyncService
    {
        private readonly IServiceManager _serviceManager;
        public SyncService(IServiceManager serviceManager)
        {
            _serviceManager = serviceManager;
        }
        public async Task<List<VersionResponseDto>> CheckVersions(VersionCheckRequestDto dto)
        {
            var tasks = new[]
            {
                GetModuleVersionAsync("Service",
                    () => _serviceManager.ServiceService.CheckVersionChange(dto.ServiceVersion),
                    () => _serviceManager.ServiceService.GetVersionAsync()),
                GetModuleVersionAsync("Story",
                    () => _serviceManager.StoryService.CheckVersionChange(dto.StoryVersion),
                    () => _serviceManager.StoryService.GetVersionAsync()),
                GetModuleVersionAsync("Category",
                    () => _serviceManager.CategoryService.CheckVersionChange(dto.CategoryVersion),
                    () => _serviceManager.CategoryService.GetVersionAsync())
            };
            var results = await Task.WhenAll(tasks);
            return results.ToList();
        }


        private async Task<VersionResponseDto> GetModuleVersionAsync(string moduleName, Func<Task<bool>> checkFunc, Func<Task<int>> versionFunc)
        {
            var hasChanged = await checkFunc();
            var version = await versionFunc();

            return new VersionResponseDto
            {
                Module = moduleName,
                HasChanged = hasChanged,
                Version = version
            };
        }
    }
}
