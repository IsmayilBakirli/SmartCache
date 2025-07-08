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
        private static readonly CacheKeyHelper.EntityKeyBuilder _keys = CacheKeyHelper.Stories;

        public StoryService(IRepositoryManager repositoryManager, IRedisService redisService, ILogger<StoryService> logger)
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
            }
            return version.Value;
        }

        public async Task<bool> CheckVersionChange(int clientVersion)
        {
            var currentVersion = await GetVersionAsync();

            if (clientVersion == currentVersion)
                return false;

            return true;
        }

        private async Task IncreaseVersionAsync()
        {
            var version = await GetVersionAsync();
            version++;
            await _redisService.SetAsync(_keys.Version, version);
            _logger.LogInformation("IncreaseVersionAsync - Yeni versiya təyin olundu: {Version}", version);
        }

        public async Task<List<StoryGetDto>> GetAllAsync()
        {
            var cached = await _redisService.GetAsync<List<StoryGetDto>>(_keys.All);
            if (cached != null)
            {
                _logger.LogInformation("GetAllAsync - Məlumat cache-dən qaytarıldı.");
                return cached;
            }

            _logger.LogInformation("GetAllAsync - Cache tapılmadı, DB-dən çəkilir.");

            var data = await _repositoryManager.StoryRepository.GetAllAsync();
            if (data == null || data.Count == 0)
                throw new NotFoundException("No stories found.");

            var dtoList = data.MapToStoryGetDtos();
            await _redisService.SetAsync(_keys.All, dtoList, _cacheExpiry);
            _logger.LogInformation("GetAllAsync - DB-dən alınan məlumat cache-ə yazıldı.");

            return dtoList;
        }

        public async Task<StoryGetDto> GetByIdAsync(int id)
        {
            var cachedDetail = await _redisService.GetAsync<StoryGetDto>(_keys.Detail(id));
            if (cachedDetail != null)
            {
                _logger.LogInformation("GetByIdAsync - id: {Id} üçün məlumat cache-dən qaytarıldı.", id);
                return cachedDetail;
            }

            _logger.LogInformation("GetByIdAsync - id: {Id} üçün cache tapılmadı, DB-dən çəkilir.", id);

            var entity = await _repositoryManager.StoryRepository.FindByIdAsync(id);
            if (entity == null)
                throw new NotFoundException($"Story with id {id} not found.");

            var dto = entity.MapToStoryGetDto();
            await _redisService.SetAsync(_keys.Detail(id), dto, _cacheExpiry);
            _logger.LogInformation("GetByIdAsync - id: {Id} üçün məlumat cache-ə yazıldı.", id);

            return dto;
        }

        public async Task CreateAsync(StoryCreateDto createDto)
        {
            var entity = createDto.MapToStory();
            await _repositoryManager.StoryRepository.CreateAsync(entity);
            var dto = entity.MapToStoryGetDto();

            await _redisService.SetAsync(_keys.Detail(dto.Id), dto, _cacheExpiry);
            _logger.LogInformation("CreateAsync - Yeni story id: {Id} üçün cache-ə yazıldı.", dto.Id);

            var existingList = await _redisService.GetAsync<List<StoryGetDto>>(_keys.All);
            if (existingList != null)
            {
                existingList.Add(dto);
                await _redisService.SetAsync(_keys.All, existingList, _cacheExpiry);
                _logger.LogInformation("CreateAsync - Yeni story id: {Id} ümumi cache listinə əlavə olundu.", dto.Id);
            }

            await IncreaseVersionAsync();
        }

        public async Task UpdateAsync(StoryUpdateDto updateDto)
        {
            var existingDto = await GetByIdAsync(updateDto.Id);
            if (existingDto == null)
                throw new NotFoundException($"Story with id {updateDto.Id} not found");

            var entity = existingDto.MapToStory();
            updateDto.MapToStory(entity);
            await _repositoryManager.StoryRepository.UpdateAsync(entity);

            var updatedDto = entity.MapToStoryGetDto();
            await _redisService.SetAsync(_keys.Detail(updatedDto.Id), updatedDto, _cacheExpiry);
            _logger.LogInformation("UpdateAsync - Story id: {Id} yeniləndi və cache-ə yazıldı.", updatedDto.Id);

            await _redisService.RemoveAsync(_keys.All);
            _logger.LogInformation("UpdateAsync - Ümumi cache ('{Key}') silindi.", _keys.All);

            await IncreaseVersionAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var dto = await GetByIdAsync(id);
            var entity = dto.MapToStory();
            await _repositoryManager.StoryRepository.DeleteAsync(entity);

            await _redisService.RemoveAsync(_keys.Detail(id));
            _logger.LogInformation("DeleteAsync - Story id: {Id} üçün detail cache silindi.", id);

            await _redisService.RemoveAsync(_keys.All);
            _logger.LogInformation("DeleteAsync - Ümumi cache ('{Key}') silindi.", _keys.All);

            await IncreaseVersionAsync();
        }
    }
}
