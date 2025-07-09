using Microsoft.Extensions.Logging;
using SmartCache.Application.Common.Helpers;
using SmartCache.Application.Common.Interfaces;
using SmartCache.Application.Contracts.Repositories.Contract;
using SmartCache.Application.Contracts.Services.Contract;
using SmartCache.Application.DTOs.Category;
using SmartCache.Application.Exceptions;
using SmartCache.Application.MappingProfile;
using SmartCache.Domain.Entities;

namespace SmartCache.Persistence.Services
{
    public class CategoryService : ICategoryService
    {
        private readonly IRepositoryManager _repositoryManager;
        private readonly IRedisService _redisService;
        private readonly ILogger<CategoryService> _logger;
        private readonly TimeSpan _cacheExpiry = TimeSpan.FromMinutes(10);
        private static readonly CacheKeyHelper.EntityKeyBuilder _keys = CacheKeyHelper.Categories;


        public CategoryService(IRepositoryManager repositoryManager, IRedisService redisService, ILogger<CategoryService> logger)
        {
            _repositoryManager = repositoryManager;
            _redisService = redisService;
            _logger = logger;
        }


        public async Task<(List<CategoryGetDto>, int)> GetAllAsync()
        {
            return (await GetOrSetCacheAsync(_keys.All, async () =>
            {
                var entities = await _repositoryManager.CategoryRepository.GetAllAsync();

                if (entities == null || entities.Count == 0)
                    throw new NotFoundException("No categories found.");

                return entities.MapToCategoryGetDtos();

            }, _cacheExpiry), await GetVersionAsync());
        }


        public async Task<CategoryGetDto> GetByIdAsync(int id)
        {
            return await GetOrSetCacheAsync(_keys.Detail(id), async () =>
            {
                var entity = await _repositoryManager.CategoryRepository.FindByIdAsync(id);

                if (entity == null)
                    throw new NotFoundException($"Category with id {id} not found.");

                return entity.MapToCategoryGetDto();

            }, _cacheExpiry);
        }


        public async Task CreateAsync(CategoryCreateDto createDto)
        {
            var entity = createDto.MapToCategory();

            await _repositoryManager.CategoryRepository.CreateAsync(entity);

            var dto = entity.MapToCategoryGetDto();

            await UpdateCacheAfterCreateAsync(dto);
        }


        public async Task UpdateAsync(CategoryUpdateDto updateDto)
        {
            var entity = await GetEntityFromUpdateDtoAsync(updateDto);

            UpdateEntityFromDto(entity, updateDto);

            await _repositoryManager.CategoryRepository.UpdateAsync(entity);

            await UpdateCacheAsync(entity);
        }


        public async Task DeleteAsync(int id)
        {
            await ValidateDeleteAsync(id);

            var entity = await GetEntityByIdAsync(id);

            await _repositoryManager.CategoryRepository.DeleteAsync(entity);

            await ClearCacheAfterDeleteAsync(id);
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


        private async Task IncreaseVersionAsync()
        {
            var version = await GetVersionAsync();

            version++;

            await _redisService.SetAsync(_keys.Version, version);

            _logger.LogInformation("Cache version increased to: {Version}", version);
        }


        private async Task UpdateCacheAfterCreateAsync(CategoryGetDto dto)
        {
            await SetCacheAsync(_keys.Detail(dto.Id), dto);

            var existingList = await GetCacheAsync<List<CategoryGetDto>>(_keys.All);

            if (existingList != null)
            {
                existingList.Add(dto);

                await SetCacheAsync(_keys.All, existingList);

                _logger.LogInformation("UpdateCacheAfterCreateAsync - New category id: {Id} added to the all-categories cache list.", dto.Id);
            }

            await IncreaseVersionAsync();

            _logger.LogInformation("New category created. Id: {Id}", dto.Id);
        }


        private async Task<Category> GetEntityFromUpdateDtoAsync(CategoryUpdateDto updateDto)
        {
            var existingDto = await GetByIdAsync(updateDto.Id);

            if (existingDto == null)
                throw new NotFoundException($"Category with id {updateDto.Id} not found.");

            return existingDto.MapToCategory();
        }


        private void UpdateEntityFromDto(Category entity, CategoryUpdateDto updateDto)
        {
            updateDto.MapToCategory(entity);
        }


        private async Task UpdateCacheAsync(Category entity)
        {

            var updatedDto = entity.MapToCategoryGetDto();

            await SetCacheAsync(_keys.Detail(updatedDto.Id), updatedDto);

            _logger.LogInformation("Category updated. Id: {Id}", updatedDto.Id);

            await RemoveCacheAsync(_keys.All);

            _logger.LogInformation("All categories cache cleared.");

            await IncreaseVersionAsync();
        }


        private async Task ValidateDeleteAsync(int id)
        {
            var existingDto = await GetByIdAsync(id);

            if (existingDto == null)
            {
                _logger.LogWarning("Category to delete not found. Id: {Id}", id);
                throw new NotFoundException($"Category with id {id} not found.");
            }

            if (!await _repositoryManager.CategoryRepository.CanDeleteCategoryAsync(id))
            {
                _logger.LogWarning("Category cannot be deleted because it is in use. Id: {Id}", id);
                throw new BadRequestException("Category is used by other services and cannot be deleted.");
            }
        }


        private async Task<Category> GetEntityByIdAsync(int id)
        {
            var entity = await _repositoryManager.CategoryRepository.FindByIdAsync(id);

            if (entity == null)
                throw new NotFoundException($"Category with id {id} not found in database.");

            return entity;
        }


        private async Task ClearCacheAfterDeleteAsync(int id)
        {
            await RemoveCacheAsync(_keys.Detail(id));

            await RemoveCacheAsync(_keys.All);

            await IncreaseVersionAsync();

            _logger.LogInformation("Category successfully deleted. Id: {Id}", id);

            _logger.LogInformation("Cache cleared after delete for category id: {Id}", id);
        }
    }
}
