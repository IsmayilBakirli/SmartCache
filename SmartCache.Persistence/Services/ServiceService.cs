using Microsoft.Extensions.Logging;
using SmartCache.Application.Common.Helpers;
using SmartCache.Application.Common.Interfaces;
using SmartCache.Application.Contracts.Repositories.Contract;
using SmartCache.Application.Contracts.Services.Contract;
using SmartCache.Application.DTOs.Category;
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

        private static readonly string VersionKey = "services:version";
        private static readonly string AllKey = CacheKeyHelper.GetAllKey("services");

        public ServiceService(IRepositoryManager repositoryManager, IRedisService redisService, ILogger<ServiceService> logger)
        {
            _repositoryManager = repositoryManager;
            _redisService = redisService;
            _logger = logger;
        }

        private string GetDetailKey(int id)
        {
            return CacheKeyHelper.GetDetailKey("services", id);
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

        public async Task<List<ServiceGetDto>> GetAllAsync()
        {
            var cached = await _redisService.GetAsync<List<ServiceGetDto>>(AllKey);
            if (cached != null)
                return cached;

            _logger.LogInformation("DB-dən GetAllAsync çağırıldı. skip: {Skip}, take: {Take}");

            var data = await _repositoryManager.ServiceRepository.GetAllAsync();
            if (data == null || data.Count == 0)
                throw new NotFoundException("No service found.");

            var dtoList = data.MapToServiceGetDtos();
            await _redisService.SetAsync(AllKey, dtoList, _cacheExpiry);

            return dtoList;
        }

        public async Task<ServiceGetDto> GetByIdAsync(int id)
        {
            var cachedList = await _redisService.GetAsync<List<ServiceGetDto>>(AllKey);
            if (cachedList != null)
            {
                var item = cachedList.FirstOrDefault(x => x.Id == id);
                if (item != null)
                    return item;
            }

            var detailKey = GetDetailKey(id);
            var cachedDetail = await _redisService.GetAsync<ServiceGetDto>(detailKey);
            if (cachedDetail != null)
                return cachedDetail;

            _logger.LogInformation("GetByIdAsync - Cache tapılmadı, DB-dən çəkilir. id: {Id}", id);

            var entity = await _repositoryManager.ServiceRepository.FindByIdAsync(id);
            if (entity == null)
                throw new NotFoundException($"No service found with {id} id.");

            var dto = entity.MapToServiceGetDto();

            await _redisService.SetAsync(detailKey, dto, _cacheExpiry);

            return dto;
        }



        public async Task CreateAsync(ServiceCreateDto createDto)
        {
            var category = await _repositoryManager.CategoryRepository.FindByIdAsync(createDto.CategoryId);
            if (category == null)
                throw new NotFoundException($"Category with id {createDto.CategoryId} not found");

            var entity = createDto.MapToService();
            await _repositoryManager.ServiceRepository.CreateAsync(entity);
            var dto = entity.MapToServiceGetDto();
            var detailKey =GetDetailKey(entity.Id);
            var existingList = await _redisService.GetAsync<List<ServiceGetDto>>(AllKey);
            if (existingList != null)
            {
                existingList.Add(dto); // 🔥 siyahıya əlavə et
                await _redisService.SetAsync(AllKey, existingList, _cacheExpiry); // təkrar yaz
            }
            else
            {
                // Cache yoxdursa, yeni siyahı yarat
                await _redisService.SetAsync(AllKey, new List<ServiceGetDto> { dto }, _cacheExpiry);
            }
            await IncreaseVersionAsync();
        }

        public async Task UpdateAsync(ServiceUpdateDto updateDto)
        {
            var existingDto = await GetByIdAsync(updateDto.Id);
            var category = await _repositoryManager.CategoryRepository.FindByIdAsync(updateDto.CategoryId);
            if (category == null)
                throw new NotFoundException($"Category with id {updateDto.CategoryId} not found");

            var entity = existingDto.MapToService();
            updateDto.MapToService(entity);
            await _repositoryManager.ServiceRepository.UpdateAsync(entity);

            var updatedDto = entity.MapToServiceGetDto();
            await _redisService.SetAsync(GetDetailKey(updatedDto.Id), updatedDto, _cacheExpiry);
            await _redisService.RemoveAsync(AllKey);
            await IncreaseVersionAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var dto = await GetByIdAsync(id);
            var entity = dto.MapToService();
            await _repositoryManager.ServiceRepository.DeleteAsync(entity);

            await _redisService.RemoveAsync(GetDetailKey(id));
            await _redisService.RemoveAsync(AllKey);
            await IncreaseVersionAsync();
        }
    }
}
