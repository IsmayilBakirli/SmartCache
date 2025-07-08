using Microsoft.Extensions.Logging;
using SmartCache.Application.Common.Helpers;
using SmartCache.Application.Common.Interfaces;
using SmartCache.Application.Contracts.Repositories.Contract;
using SmartCache.Application.Contracts.Services.Contract;
using SmartCache.Application.DTOs.Category;
using SmartCache.Application.Exceptions;
using SmartCache.Application.MappingProfile;

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
                return (entities == null || entities.Count == 0)
                    ? throw new NotFoundException("No categories found.")
                    : entities.MapToCategoryGetDtos();
            }), await GetVersionAsync());
        }

        public async Task<CategoryGetDto> GetByIdAsync(int id)
        {
            return await GetOrSetCacheAsync(_keys.Detail(id), async () =>
            {
                var entity = await _repositoryManager.CategoryRepository.FindByIdAsync(id);
                return entity == null
                    ? throw new NotFoundException($"Category with id {id} not found.")
                    : entity.MapToCategoryGetDto();
            });
        }

        public async Task CreateAsync(CategoryCreateDto createDto)
        {
            var entity = createDto.MapToCategory();
            await _repositoryManager.CategoryRepository.CreateAsync(entity);
            var dto = entity.MapToCategoryGetDto();
            await SetCacheAsync(_keys.Detail(dto.Id), dto);
            _logger.LogInformation("New category created. Id: {Id}", dto.Id);

            var existingList = await _redisService.GetAsync<List<CategoryGetDto>>(_keys.All);
            if (existingList != null)
            {
                existingList.Add(dto);
                await _redisService.SetAsync(_keys.All, existingList, _cacheExpiry);
                _logger.LogInformation("CreateAsync - New category id: {Id} added to the all-categories cache list.", dto.Id);
            }
            await IncreaseVersionAsync();
        }

        public async Task UpdateAsync(CategoryUpdateDto updateDto)
        {
            var existingDto = await GetByIdAsync(updateDto.Id); // Reading from cache
            var entity = existingDto.MapToCategory(); // Careful here

            updateDto.MapToCategory(entity);
            await _repositoryManager.CategoryRepository.UpdateAsync(entity);

            var updatedDto = entity.MapToCategoryGetDto();
            await _redisService.SetAsync(_keys.Detail(updatedDto.Id), updatedDto, _cacheExpiry);
            _logger.LogInformation("Category updated. Id: {Id}", updatedDto.Id);
            await _redisService.RemoveAsync(_keys.All);
            _logger.LogInformation("All categories cache cleared.");
            await IncreaseVersionAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var existingDto = await GetByIdAsync(id);
            if (existingDto == null)
            {
                _logger.LogWarning("Category to delete not found. Id: {Id}", id);
                throw new NotFoundException($"Category with id {id} not found.");
            }

            var entity = await _repositoryManager.CategoryRepository.FindByIdAsync(id)
                ?? throw new NotFoundException($"Category with id {id} not found in database.");

            if (!await _repositoryManager.CategoryRepository.CanDeleteCategoryAsync(id))
            {
                _logger.LogWarning("Category cannot be deleted because it is in use. Id: {Id}", id);
                throw new BadRequestException("Category is used by other services and cannot be deleted.");
            }

            await _repositoryManager.CategoryRepository.DeleteAsync(entity);
            await RemoveCacheAsync(_keys.Detail(id));
            await RemoveCacheAsync(_keys.All);
            await IncreaseVersionAsync();

            _logger.LogInformation("Category successfully deleted. Id: {Id}", id);
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

            await _redisService.SetAsync(key, result, expiration ?? TimeSpan.FromMinutes(10));
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
            var version = await _redisService.GetAsync<int?>(_keys.Version) ?? 0;
            version++;
            await _redisService.SetAsync(_keys.Version, version);
            _logger.LogInformation("Cache version increased to: {Version}", version);
        }
    }
}
