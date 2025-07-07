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
        private static readonly string VersionKey = "stories:version";

        public StoryService(IRepositoryManager repositoryManager, IRedisService redisService, ILogger<StoryService> logger)
        {
            _repositoryManager = repositoryManager;
            _redisService = redisService;
            _logger = logger;
        }

        private string GetDetailKey(int id)
        {
            return CacheKeyHelper.GetDetailKey("stories", id);
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
                throw new BadRequestException("No changes detected.");

            return currentVersion;
        }

        private async Task IncreaseVersionAsync()
        {
            var version = await GetVersionAsync();
            version++;
            await _redisService.SetAsync(VersionKey, version);
        }

        public async Task<List<StoryGetDto>> GetAllAsync()
        {
            var cached = await _redisService.GetAsync<List<StoryGetDto>>(AllKey);
            if (cached != null)
                return cached.ToList();

            _logger.LogInformation("GetAllAsync - Redis boşdur, DB-yə sorğu göndərilir.");

            var data = await _repositoryManager.StoryRepository.GetAllAsync();
            if (data == null || data.Count == 0)
                throw new NotFoundException("No stories found.");

            var dtoList = data.MapToStoryGetDtos();
            await _redisService.SetAsync(AllKey, dtoList, _cacheExpiry);

            return dtoList.ToList();
        }

        public async Task<StoryGetDto> GetByIdAsync(int id)
        {
            var cachedList = await _redisService.GetAsync<List<StoryGetDto>>(AllKey);
            if (cachedList != null)
            {
                var item = cachedList.FirstOrDefault(x => x.Id == id);
                if (item != null)
                    return item;
            }
            var detailKey = GetDetailKey(id);
            var cachedDetail = await _redisService.GetAsync<StoryGetDto>(detailKey);
            if (cachedDetail != null)
                return cachedDetail;

            _logger.LogInformation("GetByIdAsync - Cache tapılmadı, DB-dən çəkilir. id: {Id}", id);
            var entity = await _repositoryManager.StoryRepository.FindByIdAsync(id);
            if (entity == null)
                throw new NotFoundException($"Story with id {id} not found.");

            var dto = entity.MapToStoryGetDto();

            // Yalnız detailKey cache-yə yazılır
            await _redisService.SetAsync(detailKey, dto, _cacheExpiry);

            // AllKey üçün heç bir əməliyyat edilmir (partial list yazma!)
            // await _redisService.SetAsync(AllKey, ... ) kodu çıxarılır.

            return dto;
        }


        public async Task CreateAsync(StoryCreateDto createDto)
        {
            var entity = createDto.MapToStory();
            await _repositoryManager.StoryRepository.CreateAsync(entity);

            var dto = entity.MapToStoryGetDto();
            var detailKey = GetDetailKey(dto.Id);

            // Detail cache yazılır
            await _redisService.SetAsync(detailKey, dto, _cacheExpiry);

            // AllKey varsa, siyahıya əlavə olunur
            var existingList = await _redisService.GetAsync<List<StoryGetDto>>(AllKey);
            if (existingList != null)
            {
                existingList.Add(dto); // Yeni elementi əlavə et
                await _redisService.SetAsync(AllKey, existingList, _cacheExpiry);
            }
            else
            {
                // Cache mövcud deyilsə, heç nə etmirik (və ya yeni siyahı yarada bilərik)
                await _redisService.RemoveAsync(AllKey); // bu da kifayətdir əslində
            }

            await IncreaseVersionAsync();
        }


        public async Task UpdateAsync(StoryUpdateDto updateDto)
        {
            var detailKey = GetDetailKey(updateDto.Id);
            var existingDto = await GetByIdAsync(updateDto.Id);
            if (existingDto == null)
                throw new NotFoundException($"Story with id {updateDto.Id} not found");

            var entity = existingDto.MapToStory();
            updateDto.MapToStory(entity);
            await _repositoryManager.StoryRepository.UpdateAsync(entity);

            var updatedDto = entity.MapToStoryGetDto();
            await _redisService.SetAsync(detailKey, updatedDto, _cacheExpiry);
            await _redisService.RemoveAsync(AllKey);
            await IncreaseVersionAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var dto = await GetByIdAsync(id);
            var entity = dto.MapToStory();

            await _repositoryManager.StoryRepository.DeleteAsync(entity);

            var detailKey = GetDetailKey(id);
            await _redisService.RemoveAsync(detailKey);
            await _redisService.RemoveAsync(AllKey);
            await IncreaseVersionAsync();
        }
    }
}
