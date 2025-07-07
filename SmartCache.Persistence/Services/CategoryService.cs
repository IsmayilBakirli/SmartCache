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

        public CategoryService(IRepositoryManager repositoryManager, IRedisService redisService, ILogger<CategoryService> logger)
        {
            _repositoryManager = repositoryManager;
            _redisService = redisService;
            _logger = logger;
        }

        public async Task<List<CategoryGetDto>> GetAllAsync(int skip = 0, int take = int.MaxValue)
        {
            var cached = await _redisService.GetAsync<List<CategoryGetDto>>(AllKey);
            if (cached != null)
                return cached.Skip(skip).Take(take).ToList();

            _logger.LogInformation("GetAllAsync - Redis boşdur, DB-yə sorğu göndərilir.");

            var data = await _repositoryManager.CategoryRepository.GetAllAsync(0, int.MaxValue);
            if (data == null || data.Count == 0)
                throw new NotFoundException("No category found.");

            var dtoList = data.MapToCategoryGetDtos();
            await _redisService.SetAsync(AllKey, dtoList, _cacheExpiry);

            return dtoList.Skip(skip).Take(take).ToList();
        }

        public async Task<CategoryGetDto> GetByIdAsync(int id)
        {
            var cached = await _redisService.GetAsync<List<CategoryGetDto>>(AllKey);
            if (cached != null)
            {
                var item = cached.FirstOrDefault(x => x.Id == id);
                if (item != null)
                    return item;

                throw new NotFoundException($"Category with id {id} not found in cache.");
            }

            _logger.LogInformation("GetByIdAsync - Cache tapılmadı, DB-yə sorğu göndərilir. id: {Id}", id);

            var entity = await _repositoryManager.CategoryRepository.FindByIdAsync(id);
            if (entity == null)
                throw new NotFoundException($"Category with id {id} not found.");

            var dto = entity.MapToCategoryGetDto();
            await _redisService.SetAsync(AllKey, new List<CategoryGetDto> { dto }, _cacheExpiry);

            return dto;
        }

        public async Task CreateAsync(CategoryCreateDto createDto)
        {
            var entity = createDto.MapToCategory();
            await _repositoryManager.CategoryRepository.CreateAsync(entity);
            var dto = entity.MapToCategoryGetDto();

            var cached = await _redisService.GetAsync<List<CategoryGetDto>>(AllKey) ?? new List<CategoryGetDto>();
            cached.Add(dto);
            await _redisService.SetAsync(AllKey, cached, _cacheExpiry);
        }

        public async Task UpdateAsync(CategoryUpdateDto updateDto)
        {
            var cached = await _redisService.GetAsync<List<CategoryGetDto>>(AllKey);
            if (cached == null)
                throw new NotFoundException("Category listesi cache-də tapılmadı.");

            var index = cached.FindIndex(x => x.Id == updateDto.Id);
            if (index == -1)
                throw new NotFoundException($"Category with id {updateDto.Id} not found");

            var entity = cached[index].MapToCategory();
            updateDto.MapToCategory(entity);
            await _repositoryManager.CategoryRepository.UpdateAsync(entity);

            cached[index] = entity.MapToCategoryGetDto();
            await _redisService.SetAsync(AllKey, cached, _cacheExpiry);
        }

        public async Task DeleteAsync(int id)
        {
            var cached = await _redisService.GetAsync<List<CategoryGetDto>>(AllKey);
            if (cached == null)
                throw new NotFoundException("Category listesi cache-də tapılmadı.");

            var dto = cached.FirstOrDefault(x => x.Id == id);
            if (dto == null)
                throw new NotFoundException($"Category with id {id} not found");

            if (!await _repositoryManager.CategoryRepository.CanDeleteCategoryAsync(id))
                throw new BadRequestException("This category is used by one or more services and cannot be deleted.");

            var entity = dto.MapToCategory();
            await _repositoryManager.CategoryRepository.DeleteAsync(entity);

            cached.RemoveAll(x => x.Id == id);
            await _redisService.SetAsync(AllKey, cached, _cacheExpiry);
        }
    }
}
