using Microsoft.Extensions.Logging;
using SmartCache.Application.Common.Helpers;
using SmartCache.Application.Common.Interfaces;
using SmartCache.Application.Contracts.Repositories.Contract;
using SmartCache.Application.Contracts.Services.Contract;
using SmartCache.Application.DTOs.Service;
using SmartCache.Application.Exceptions;
using SmartCache.Application.MappingProfile;

namespace SmartCache.Persistence.Services
{
    public class ServiceService : IServiceService
    {
        private readonly IRepositoryManager _repositoryManager;
        private readonly IRedisService _redisService;
        private readonly ILogger<ServiceService> _logger;
        private readonly TimeSpan _cacheExpiry = TimeSpan.FromMinutes(10);
        private static readonly CacheKeyHelper.EntityKeyBuilder _keys = CacheKeyHelper.Services;

        public ServiceService(IRepositoryManager repositoryManager, IRedisService redisService, ILogger<ServiceService> logger)
        {
            _repositoryManager = repositoryManager;
            _redisService = redisService;
            _logger = logger;
        }
        public async Task<(List<ServiceGetDto>,int)> GetAllAsync()
        {
            return (await GetOrSetCacheAsync(_keys.All, async () =>
            {
                var data = await _repositoryManager.ServiceRepository.GetAllAsync();
                if (data == null || data.Count == 0)
                    throw new NotFoundException("No services found.");

                return data.MapToServiceGetDtos();
            }), await GetVersionAsync());
        }

        public async Task<ServiceGetDto> GetByIdAsync(int id)
        {
            return await GetOrSetCacheAsync(_keys.Detail(id), async () =>
            {
                var entity = await _repositoryManager.ServiceRepository.FindByIdAsync(id);
                if (entity == null)
                    throw new NotFoundException($"Service with id {id} not found.");

                return entity.MapToServiceGetDto();
            });
        }
        public async Task CreateAsync(ServiceCreateDto createDto)
        {
            var category = await _repositoryManager.CategoryRepository.FindByIdAsync(createDto.CategoryId);
            if (category == null)
                throw new NotFoundException($"Category with id {createDto.CategoryId} not found.");

            var entity = createDto.MapToService();
            await _repositoryManager.ServiceRepository.CreateAsync(entity);

            var createdEntity = await _repositoryManager.ServiceRepository.FindByIdAsync(entity.Id);
            var dto = createdEntity.MapToServiceGetDto();

            await SetCacheAsync(_keys.Detail(dto.Id), dto);
            _logger.LogInformation("New service created and cached. Id: {Id}", dto.Id);

            var existingList = await GetCacheAsync<List<ServiceGetDto>>(_keys.All);
            if (existingList != null)
            {
                existingList.Add(dto);
                await SetCacheAsync(_keys.All, existingList);
                _logger.LogInformation("New service added to all-services cache. Id: {Id}", dto.Id);
            }

            await IncreaseVersionAsync();
        }

        public async Task UpdateAsync(ServiceUpdateDto updateDto)
        {
            var existingDto = await GetByIdAsync(updateDto.Id);

            var category = await _repositoryManager.CategoryRepository.FindByIdAsync(updateDto.CategoryId);
            if (category == null)
                throw new NotFoundException($"Category with id {updateDto.CategoryId} not found.");

            var entity = existingDto.MapToService();
            updateDto.MapToService(entity);
            await _repositoryManager.ServiceRepository.UpdateAsync(entity);

            var updatedDto = entity.MapToServiceGetDto();

            await SetCacheAsync(_keys.Detail(updatedDto.Id), updatedDto);
            _logger.LogInformation("Service updated and cached. Id: {Id}", updatedDto.Id);

            await RemoveCacheAsync(_keys.All);
            _logger.LogInformation("All-services cache cleared.");

            await IncreaseVersionAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var dto = await GetByIdAsync(id);
            var entity = dto.MapToService();

            await _repositoryManager.ServiceRepository.DeleteAsync(entity);

            await RemoveCacheAsync(_keys.Detail(id));
            _logger.LogInformation("Detail cache removed for service id: {Id}", id);

            await RemoveCacheAsync(_keys.All);
            _logger.LogInformation("All-services cache cleared.");

            await IncreaseVersionAsync();
        }
        public async Task<int> GetVersionAsync()
        {
            var version = await _redisService.GetAsync<int?>(_keys.Version) ?? 0;
            if (version == 0)
                await _redisService.SetAsync(_keys.Version, 0);

            _logger.LogInformation(version == 0
                ? "Version initialized to 0."
                : "Current version: {Version}", version);

            return version;
        }

        public async Task<bool> CheckVersionChange(int clientVersion)
        {
            var currentVersion = await GetVersionAsync();
            var hasChanged = clientVersion != currentVersion;

            if (!hasChanged)
                _logger.LogInformation("No version change detected. Client version: {ClientVersion}", clientVersion);

            return hasChanged;
        }

        private async Task IncreaseVersionAsync()
        {
            var version = await GetVersionAsync();
            version++;
            await _redisService.SetAsync(_keys.Version, version);
            _logger.LogInformation("Cache version increased to: {Version}", version);
        }





        public async Task<T> GetOrSetCacheAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null)
        {
            var cached = await _redisService.GetAsync<T>(key);
            if (cached != null)
            {
                _logger.LogInformation("Cache hit for key: {Key}", key);
                return cached;
            }

            _logger.LogInformation("Cache miss for key: {Key}. Loading from database...", key);

            var result = await factory();

            await _redisService.SetAsync(key, result, expiration ?? _cacheExpiry);
            _logger.LogInformation("Data cached for key: {Key}", key);

            return result;
        }

        private async Task<T?> GetCacheAsync<T>(string key)
        {
            var cached = await _redisService.GetAsync<T>(key);

            _logger.LogInformation(cached != null
                ? "Cache hit for key: {Key}"
                : "Cache miss for key: {Key}", key);

            return cached;
        }

        private async Task SetCacheAsync<T>(string key, T value)
        {
            await _redisService.SetAsync(key, value, _cacheExpiry);
            _logger.LogInformation("Cache set for key: {Key}", key);
        }

        private async Task RemoveCacheAsync(string key)
        {
            await _redisService.RemoveAsync(key);
            _logger.LogInformation("Cache removed for key: {Key}", key);
        }
    }
}
