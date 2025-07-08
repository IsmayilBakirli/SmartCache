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

        // Cache açarlarını təyin etmək üçün
        private static readonly CacheKeyHelper.EntityKeyBuilder _keys = CacheKeyHelper.Categories;

        public CategoryService(IRepositoryManager repositoryManager, IRedisService redisService, ILogger<CategoryService> logger)
        {
            _repositoryManager = repositoryManager;
            _redisService = redisService;
            _logger = logger;
        }

        public async Task<int> GetVersionAsync()
        {
            var version = await _redisService.GetAsync<int?>(_keys.Version);
            if (version == null)
            {
                version = 0;
                await _redisService.SetAsync(_keys.Version, version.Value);
                _logger.LogInformation("GetVersionAsync - Versiya mövcud deyildi, 0 olaraq təyin olundu.");
            }
            else
            {
                _logger.LogInformation("GetVersionAsync - Cari versiya: {Version}", version.Value);
            }
            return version.Value;
        }

        public async Task<bool> CheckVersionChange(int clientVersion)
        {
            var currentVersion = await GetVersionAsync();
            if (clientVersion == currentVersion)
            {
                _logger.LogInformation("CheckVersionAsync - Heç bir dəyişiklik yoxdur. ClientVersion = {ClientVersion}", clientVersion);
                return false;
            }
            return true ;
        }

        private async Task IncreaseVersionAsync()
        {
            var version = await GetVersionAsync();
            version++;
            await _redisService.SetAsync(_keys.Version, version);
            _logger.LogInformation("IncreaseVersionAsync - Versiya artırıldı. Yeni versiya: {Version}", version);
        }

        public async Task<List<CategoryGetDto>> GetAllAsync()
        {
            var cached = await _redisService.GetAsync<List<CategoryGetDto>>(_keys.All);
            if (cached != null)
            {
                _logger.LogInformation("GetAllAsync - Məlumat cache-dən qaytarıldı. Say: {Count}", cached.Count);
                return cached;
            }

            _logger.LogInformation("GetAllAsync - Cache tapılmadı, DB-dən məlumat çəkilir.");

            var data = await _repositoryManager.CategoryRepository.GetAllAsync();
            if (data == null || data.Count == 0)
            {
                _logger.LogWarning("GetAllAsync - DB-də category tapılmadı.");
                throw new NotFoundException("No category found.");
            }

            var dtoList = data.MapToCategoryGetDtos();

            await _redisService.SetAsync(_keys.All, dtoList, _cacheExpiry);
            _logger.LogInformation("GetAllAsync - DB-dən alınan məlumat cache-ə yazıldı. Say: {Count}", dtoList.Count);

            return dtoList;
        }

        public async Task<CategoryGetDto> GetByIdAsync(int id)
        {
            var detailKey = _keys.Detail(id);

            var cachedDetail = await _redisService.GetAsync<CategoryGetDto>(detailKey);
            if (cachedDetail != null)
            {
                _logger.LogInformation("GetByIdAsync - id: {Id} üçün məlumat cache-dən qaytarıldı.", id);
                return cachedDetail;
            }

            _logger.LogInformation("GetByIdAsync - Cache tapılmadı, DB-dən məlumat çəkilir. id: {Id}", id);

            var entity = await _repositoryManager.CategoryRepository.FindByIdAsync(id);
            if (entity == null)
            {
                _logger.LogWarning("GetByIdAsync - id: {Id} üçün category tapılmadı.", id);
                throw new NotFoundException($"Category with id {id} not found.");
            }

            var dto = entity.MapToCategoryGetDto();
            await _redisService.SetAsync(detailKey, dto, _cacheExpiry);

            return dto;
        }

        public async Task CreateAsync(CategoryCreateDto createDto)
        {
            var entity = createDto.MapToCategory();
            await _repositoryManager.CategoryRepository.CreateAsync(entity);

            var dto = entity.MapToCategoryGetDto();
            var detailKey = _keys.Detail(dto.Id);

            await _redisService.SetAsync(detailKey, dto, _cacheExpiry);
            _logger.LogInformation("CreateAsync - Yeni category id: {Id} üçün detail cache-ə yazıldı.", dto.Id);

            var existingList = await _redisService.GetAsync<List<CategoryGetDto>>(_keys.All);
            if (existingList != null)
            {
                existingList.Add(dto);
                await _redisService.SetAsync(_keys.All, existingList, _cacheExpiry);
                _logger.LogInformation("CreateAsync - Yeni category id: {Id} ümumi cache listinə əlavə olundu.", dto.Id);
            }

            await IncreaseVersionAsync();
        }

        public async Task UpdateAsync(CategoryUpdateDto updateDto)
        {
            var detailKey = _keys.Detail(updateDto.Id);
            var existingDto = await GetByIdAsync(updateDto.Id);
            if (existingDto == null)
            {
                _logger.LogWarning("UpdateAsync - Category id: {Id} tapılmadı.", updateDto.Id);
                throw new NotFoundException($"Category with id {updateDto.Id} not found");
            }

            var entity = existingDto.MapToCategory();
            updateDto.MapToCategory(entity);
            await _repositoryManager.CategoryRepository.UpdateAsync(entity);

            var updatedDto = entity.MapToCategoryGetDto();
            await _redisService.SetAsync(detailKey, updatedDto, _cacheExpiry);
            _logger.LogInformation("UpdateAsync - Category id: {Id} yeniləndi və cache-ə yazıldı.", updatedDto.Id);

            await _redisService.RemoveAsync(_keys.All);
            _logger.LogInformation("UpdateAsync - Ümumi cache ('{Key}') silindi.", _keys.All);

            await IncreaseVersionAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var dto = await GetByIdAsync(id);
            if (dto == null)
            {
                _logger.LogWarning("DeleteAsync - Category id: {Id} tapılmadı.", id);
                throw new NotFoundException($"Category with id {id} not found");
            }

            if (!await _repositoryManager.CategoryRepository.CanDeleteCategoryAsync(id))
            {
                _logger.LogWarning("DeleteAsync - Category id: {Id} başqa servislər tərəfindən istifadə olunur və silinə bilməz.", id);
                throw new BadRequestException("This category is used by one or more services and cannot be deleted.");
            }

            var entity = dto.MapToCategory();
            await _repositoryManager.CategoryRepository.DeleteAsync(entity);

            var detailKey = _keys.Detail(id);
            await _redisService.RemoveAsync(detailKey);
            _logger.LogInformation("DeleteAsync - Category id: {Id} üçün detail cache silindi.", id);

            await _redisService.RemoveAsync(_keys.All);
            _logger.LogInformation("DeleteAsync - Ümumi cache ('{Key}') silindi.", _keys.All);

            await IncreaseVersionAsync();
        }
    }
}
