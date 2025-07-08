using Microsoft.Extensions.Logging;
using SmartCache.Application.Common.Helpers;
using SmartCache.Application.Common.Interfaces;
using SmartCache.Application.Contracts.Repositories.Contract;
using SmartCache.Application.Contracts.Services.Contract;
using SmartCache.Application.DTOs.Story;
using SmartCache.Application.Exceptions;
using SmartCache.Application.MappingProfile;
using SmartCache.Domain.Entities;

namespace SmartCache.Persistence.Services
{
    public class StoryService : IStoryService
    {
        private readonly IRepositoryManager _repositoryManager;
        private readonly IRedisService _redisService;
        private readonly ILogger<StoryService> _logger;
        private readonly TimeSpan _cacheExpiry = TimeSpan.FromMinutes(10);
        private static readonly CacheKeyHelper.EntityKeyBuilder _keys = CacheKeyHelper.Stories;


        public StoryService(IRepositoryManager repositoryManager, IRedisService redisService, ILogger<StoryService> logger)
        {
            _repositoryManager = repositoryManager;
            _redisService = redisService;
            _logger = logger;
        }


        public async Task<(List<StoryGetDto>, int)> GetAllAsync()
        {
            return (await GetOrSetCacheAsync(_keys.All, async () =>
            {
                var data = await _repositoryManager.StoryRepository.GetAllAsync();
                if (data == null || data.Count == 0)
                    throw new NotFoundException("No stories found.");
                return data.MapToStoryGetDtos();
            }, _cacheExpiry), await GetVersionAsync());
        }


        public async Task CreateAsync(StoryCreateDto createDto)
        {
            var entity = createDto.MapToStory();
            await _repositoryManager.StoryRepository.CreateAsync(entity);
            var dto = entity.MapToStoryGetDto();
            await UpdateCacheAfterCreateAsync(dto);
        }


        public async Task UpdateAsync(StoryUpdateDto updateDto)
        {
            var entity = await GetEntityFromUpdateDtoAsync(updateDto);
            UpdateEntityFromDto(entity, updateDto);
            await _repositoryManager.StoryRepository.UpdateAsync(entity);
            await UpdateCacheAsync(entity);
        }


        public async Task DeleteAsync(int id)
        {
            var dto = await GetByIdAsync(id);
            var entity = dto.MapToStory();
            await _repositoryManager.StoryRepository.DeleteAsync(entity);
            await ClearCacheAfterDeleteAsync(id);
        }


        public async Task<StoryGetDto> GetByIdAsync(int id)
        {
            return await GetOrSetCacheAsync(_keys.Detail(id), async () =>
            {
                var entity = await _repositoryManager.StoryRepository.FindByIdAsync(id);
                if (entity == null)
                    throw new NotFoundException($"Story with id {id} not found.");
                return entity.MapToStoryGetDto();
            }, _cacheExpiry);
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


        // Generic cache helper methods


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


        private async Task UpdateCacheAfterCreateAsync(StoryGetDto dto)
        {
            await SetCacheAsync(_keys.Detail(dto.Id), dto);

            var existingList = await GetCacheAsync<List<StoryGetDto>>(_keys.All);
            if (existingList != null)
            {
                existingList.Add(dto);
                await SetCacheAsync(_keys.All, existingList);
                _logger.LogInformation("UpdateCacheAfterCreateAsync - New story id: {Id} added to the all-stories cache list.", dto.Id);
            }
            await IncreaseVersionAsync();
        }


        private async Task<Story> GetEntityFromUpdateDtoAsync(StoryUpdateDto updateDto)
        {
            var existingDto = await GetByIdAsync(updateDto.Id);
            if (existingDto == null)
                throw new NotFoundException($"Story with id {updateDto.Id} not found.");

            var entity = existingDto.MapToStory();
            return entity;
        }


        private void UpdateEntityFromDto(Story entity, StoryUpdateDto updateDto)
        {
            updateDto.MapToStory(entity);
        }


        private async Task UpdateCacheAsync(Story entity)
        {
            var updatedDto = entity.MapToStoryGetDto();
            await SetCacheAsync(_keys.Detail(updatedDto.Id), updatedDto);
            await RemoveCacheAsync(_keys.All);
            _logger.LogInformation("UpdateAsync - All stories cache cleared.");
            await IncreaseVersionAsync();
        }


        private async Task ClearCacheAfterDeleteAsync(int id)
        {
            await RemoveCacheAsync(_keys.Detail(id));
            await RemoveCacheAsync(_keys.All);
            _logger.LogInformation("DeleteAsync - Story detail and all stories cache cleared.");
            await IncreaseVersionAsync();
        }
    }
}
