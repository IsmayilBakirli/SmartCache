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

        private static readonly string AllKey = CacheKeyHelper.GetAllKey("categories");
        private static readonly string VersionKey = "categories:version";


        public CategoryService(IRepositoryManager repositoryManager, IRedisService redisService, ILogger<CategoryService> logger)
        {
            _repositoryManager = repositoryManager;
            _redisService = redisService;
            _logger = logger;
        }
        private string GetDetailKey(int id)
        {
            return CacheKeyHelper.GetDetailKey("categories", id);
        }
        public async Task<int> GetVersionAsync()
        {
            var version = await _redisService.GetAsync<int?>(VersionKey);
            if (version == null)
            {
                version = 0;
                await _redisService.SetAsync(VersionKey, version.Value);
            }
            return version.Value;
        }

        public async Task<int> CheckVersionAsync(int clientVersion)
        {
            var currentVersion = await GetVersionAsync();

            if (clientVersion == currentVersion)
            {
                throw new BadRequestException("No changes detected.");
            }
            return currentVersion;
        }

        private async Task IncreaseVersionAsync()
        {
            var version = await GetVersionAsync();
            version++;
            await _redisService.SetAsync(VersionKey, version);
        }

        public async Task<List<CategoryGetDto>> GetAllAsync()
        {
            var cached = await _redisService.GetAsync<List<CategoryGetDto>>(AllKey);
            if (cached != null)
                return cached.ToList();

            _logger.LogInformation("DB-dən GetAllAsync çağırıldı. skip: {Skip}, take: {Take}");

            var data = await _repositoryManager.CategoryRepository.GetAllAsync();
            if (data == null || data.Count == 0)
                throw new NotFoundException("No category found.");

            var dtoList = data.MapToCategoryGetDtos();
            await _redisService.SetAsync(AllKey, dtoList, _cacheExpiry);

            return dtoList.ToList();
        }


        public async Task<CategoryGetDto> GetByIdAsync(int id)
        {
            var cachedList = await _redisService.GetAsync<List<CategoryGetDto>>(AllKey);
            if (cachedList != null)
            {
                var item = cachedList.FirstOrDefault(x => x.Id == id);
                if (item != null)
                    return item;
            }

            var detailKey =GetDetailKey(id);
            var cachedDetail = await _redisService.GetAsync<CategoryGetDto>(detailKey);
            if (cachedDetail != null)
                return cachedDetail;

            _logger.LogInformation("GetByIdAsync - Cache tapılmadı, DB-dən çəkilir. id: {Id}", id);
            var entity = await _repositoryManager.CategoryRepository.FindByIdAsync(id);
            if (entity == null)
                throw new NotFoundException($"Category with id {id} not found.");

            var dto = entity.MapToCategoryGetDto();

            // Yalnız detailKey üçün cache yeniləmə
            await _redisService.SetAsync(detailKey, dto, _cacheExpiry);

            // AllKey-ə partial siyahı əlavə etmə
            // await _redisService.SetAsync(AllKey, ...) - bu sətir çıxarıldı

            return dto;
        }


        public async Task CreateAsync(CategoryCreateDto createDto)
        {
            var entity = createDto.MapToCategory();
            await _repositoryManager.CategoryRepository.CreateAsync(entity);

            var dto = entity.MapToCategoryGetDto();
            var detailKey = GetDetailKey(dto.Id);


            // ✅ AllKey-dən siyahını çək
            var existingList = await _redisService.GetAsync<List<CategoryGetDto>>(AllKey);
            if (existingList != null)
            {
                existingList.Add(dto); // 🔥 siyahıya əlavə et
                await _redisService.SetAsync(AllKey, existingList, _cacheExpiry); // təkrar yaz
            }
            else
            {
                // Cache yoxdursa, yeni siyahı yarat
                await _redisService.SetAsync(AllKey, new List<CategoryGetDto> { dto }, _cacheExpiry);
            }
            await IncreaseVersionAsync();

        }


        public async Task UpdateAsync(CategoryUpdateDto updateDto)
        {
            var detailKey = GetDetailKey(updateDto.Id);
            var existingDto = await GetByIdAsync(updateDto.Id);
            if (existingDto == null)
                throw new NotFoundException($"Category with id {updateDto.Id} not found");

            var entity = existingDto.MapToCategory();

            updateDto.MapToCategory(entity);
            await _repositoryManager.CategoryRepository.UpdateAsync(entity);

            var updatedDto = entity.MapToCategoryGetDto();
            await _redisService.SetAsync(detailKey, updatedDto, _cacheExpiry);
            await _redisService.RemoveAsync(AllKey);
            await IncreaseVersionAsync();
        }


        public async Task DeleteAsync(int id)
        {
            var dto = await GetByIdAsync(id);
            var entity = dto.MapToCategory();

            if (!await _repositoryManager.CategoryRepository.CanDeleteCategoryAsync(id))
                throw new BadRequestException("This category is used by one or more services and cannot be deleted.");

            await _repositoryManager.CategoryRepository.DeleteAsync(entity);

            var detailKey = CacheKeyHelper.GetDetailKey("categories", id);
            await _redisService.RemoveAsync(detailKey);
            await _redisService.RemoveAsync(AllKey);
            await IncreaseVersionAsync();
        }


    }
}
