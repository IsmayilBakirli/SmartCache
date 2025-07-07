using Microsoft.Extensions.Logging;
using SmartCache.Application.Common.Helpers;
using SmartCache.Application.Common.Interfaces;
using SmartCache.Application.Contracts.Repositories.Contract;
using SmartCache.Application.Contracts.Services.Contract;
using SmartCache.Application.DTOs.Story;
using SmartCache.Application.Exceptions;
using SmartCache.Application.MappingProfile;

namespace SmartCache.Persistence.Services
{
    public class StoryService : IStoryService
    {
        private readonly IRepositoryManager _repositoryManager;
        private readonly IRedisService _redisService;
        private readonly ILogger<StoryService> _logger;
        private readonly TimeSpan _cacheExpiry = TimeSpan.FromMinutes(10);

        private static readonly string AllKey = CacheKeyHelper.GetAllKey("stories");

        public StoryService(IRepositoryManager repositoryManager, IRedisService redisService, ILogger<StoryService> logger)
        {
            _repositoryManager = repositoryManager;
            _redisService = redisService;
            _logger = logger;
        }

        public async Task<List<StoryGetDto>> GetAllAsync(int skip = 0, int take = int.MaxValue)
        {
            var cached = await _redisService.GetAsync<List<StoryGetDto>>(AllKey);
            if (cached != null)
                return cached.Skip(skip).Take(take).ToList();

            _logger.LogInformation("GetAllAsync - Redis boşdur, DB-yə sorğu göndərilir.");

            var data = await _repositoryManager.StoryRepository.GetAllAsync(0, int.MaxValue);
            if (data == null || data.Count == 0)
                throw new NotFoundException("No stories found.");

            var dtoList = data.MapToStoryGetDtos();
            await _redisService.SetAsync(AllKey, dtoList, _cacheExpiry);

            return dtoList.Skip(skip).Take(take).ToList();
        }

        public async Task<StoryGetDto> GetByIdAsync(int id)
        {
            var cached = await _redisService.GetAsync<List<StoryGetDto>>(AllKey);
            if (cached != null)
            {
                var item = cached.FirstOrDefault(x => x.Id == id);
                if (item != null)
                    return item;

                throw new NotFoundException($"Story with id {id} not found in cache.");
            }

            _logger.LogInformation("GetByIdAsync - Cache tapılmadı, DB-yə sorğu göndərilir. id: {Id}", id);

            var entity = await _repositoryManager.StoryRepository.FindByIdAsync(id);
            if (entity == null)
                throw new NotFoundException($"Story with id {id} not found.");

            var dto = entity.MapToStoryGetDto();
            await _redisService.SetAsync(AllKey, new List<StoryGetDto> { dto }, _cacheExpiry);

            return dto;
        }

        public async Task CreateAsync(StoryCreateDto createDto)
        {
            var entity = createDto.MapToStory();
            await _repositoryManager.StoryRepository.CreateAsync(entity);

            var dto = entity.MapToStoryGetDto();

            var cached = await _redisService.GetAsync<List<StoryGetDto>>(AllKey) ?? new List<StoryGetDto>();
            cached.Add(dto);
            await _redisService.SetAsync(AllKey, cached, _cacheExpiry);
        }

        public async Task UpdateAsync(StoryUpdateDto updateDto)
        {
            var cached = await _redisService.GetAsync<List<StoryGetDto>>(AllKey);
            if (cached == null)
                throw new NotFoundException("Story listesi cache-də tapılmadı.");

            var index = cached.FindIndex(x => x.Id == updateDto.Id);
            if (index == -1)
                throw new NotFoundException($"Story with id {updateDto.Id} not found");

            var entity = cached[index].MapToStory();
            updateDto.MapToStory(entity);
            await _repositoryManager.StoryRepository.UpdateAsync(entity);

            cached[index] = entity.MapToStoryGetDto();
            await _redisService.SetAsync(AllKey, cached, _cacheExpiry);
        }

        public async Task DeleteAsync(int id)
        {
            var cached = await _redisService.GetAsync<List<StoryGetDto>>(AllKey);
            if (cached == null)
                throw new NotFoundException("Story listesi cache-də tapılmadı.");

            var dto = cached.FirstOrDefault(x => x.Id == id);
            if (dto == null)
                throw new NotFoundException($"Story with id {id} not found");

            var entity = dto.MapToStory();
            await _repositoryManager.StoryRepository.DeleteAsync(entity);

            cached.RemoveAll(x => x.Id == id);
            await _redisService.SetAsync(AllKey, cached, _cacheExpiry);
        }
    }
}
