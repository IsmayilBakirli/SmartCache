using Microsoft.Extensions.Logging;
using SmartCache.Application.Common.Helpers;
using SmartCache.Application.Common.Interfaces;
using SmartCache.Application.Contracts.Repositories.Contract;
using SmartCache.Application.Contracts.Services.Contract;
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
        private static readonly CacheKeyHelper.EntityKeyBuilder _keys = CacheKeyHelper.Services;


        public ServiceService(IRepositoryManager repositoryManager, IRedisService redisService, ILogger<ServiceService> logger)
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
            {
                return false;
            }
            return true;
        }

        private async Task IncreaseVersionAsync()
        {
            var version = await GetVersionAsync();
            version++;
            await _redisService.SetAsync(_keys.Version, version);
            _logger.LogInformation("IncreaseVersionAsync - Yeni versiya təyin olundu: {Version}", version);
        }

        public async Task<List<ServiceGetDto>> GetAllAsync()
        {
            var cached = await _redisService.GetAsync<List<ServiceGetDto>>(_keys.All);
            if (cached != null)
            {
                _logger.LogInformation("GetAllAsync - Məlumat cache-dən qaytarıldı.");  
                return cached;
            }
            _logger.LogInformation("GetAllAsync - Cache tapılmadı, DB-dən çəkilir.");

            var data = await _repositoryManager.ServiceRepository.GetAllAsync();
            if (data == null || data.Count == 0)
                throw new NotFoundException("No service found.");

            var dtoList = data.MapToServiceGetDtos();
            await _redisService.SetAsync(_keys.All, dtoList, _cacheExpiry);
            _logger.LogInformation("GetAllAsync - DB-dən alınan məlumat cache-ə yazıldı.");
            return dtoList;
        }

        public async Task<ServiceGetDto> GetByIdAsync(int id)
        {
            var cachedDetail = await _redisService.GetAsync<ServiceGetDto>(_keys.Detail(id));
            if (cachedDetail != null)
            {
                _logger.LogInformation("GetByIdAsync - id: {id} üçün məlumat cache-dən qaytarıldı.", id);
                return cachedDetail;
            }

            _logger.LogInformation("GetByIdAsync - id: {id} üçün cache tapılmadı, DB-dən çəkilir.", id);

            var entity = await _repositoryManager.ServiceRepository.FindByIdAsync(id);
            if (entity == null)
                throw new NotFoundException($"No service found with {id} id.");

            var dto = entity.MapToServiceGetDto();
            await _redisService.SetAsync(_keys.Detail(id), dto, _cacheExpiry);
            _logger.LogInformation("GetByIdAsync - id: {Id} üçün məlumat cache-ə yazıldı.", id);

            return dto;
        }




        public async Task CreateAsync(ServiceCreateDto createDto)
        {
            var category = await _repositoryManager.CategoryRepository.FindByIdAsync(createDto.CategoryId);
            if (category == null)
                throw new NotFoundException($"Category with id {createDto.CategoryId} not found");

            var entity = createDto.MapToService();
            await _repositoryManager.ServiceRepository.CreateAsync(entity);

            // Yaratdıqdan sonra entity Id var, ona görə yenidən DB-dən yükləmək olar:
            var createdEntity = await _repositoryManager.ServiceRepository.FindByIdAsync(entity.Id);

            // Map edəndə category məlumatını da əlavə edirik
            var dto = createdEntity.MapToServiceGetDto();

            // CategoryName-in doldurulması (əgər MapToServiceGetDto avtomatik etmirsə)


            await _redisService.SetAsync(_keys.Detail(dto.Id), dto, _cacheExpiry);
            _logger.LogInformation("CreateAsync - Yeni service id: {Id} üçün cache-ə yazıldı.", dto.Id);

            var existingList = await _redisService.GetAsync<List<ServiceGetDto>>(_keys.All);
            if (existingList != null)
            {
                existingList.Add(dto);
                await _redisService.SetAsync(_keys.All, existingList, _cacheExpiry);
                _logger.LogInformation("CreateAsync - Yeni service id: {Id} ümumi cache listinə əlavə olundu.", dto.Id);
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

            await _redisService.SetAsync(_keys.Detail(updatedDto.Id), updatedDto, _cacheExpiry);
            _logger.LogInformation($"UpdateAsync - Service id: {updatedDto.Id} yeniləndi və cache-ə yazıldı.", updatedDto.Id);

            await _redisService.RemoveAsync(_keys.All);
            _logger.LogInformation("UpdateAsync - Ümumi cache ('{Key}') silindi.", _keys.All);

            await IncreaseVersionAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var dto = await GetByIdAsync(id);
            var entity = dto.MapToService();
            await _repositoryManager.ServiceRepository.DeleteAsync(entity);

            await _redisService.RemoveAsync(_keys.Detail(id));
            _logger.LogInformation("DeleteAsync - Service id: {Id} üçün detail cache silindi.", id);

            await _redisService.RemoveAsync(_keys.All);
            _logger.LogInformation("DeleteAsync - Ümumi cache ('{Key}') silindi.", _keys.All);

            await IncreaseVersionAsync();
        }
    }
}
