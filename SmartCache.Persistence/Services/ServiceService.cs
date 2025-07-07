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

        private static readonly string AllKey = CacheKeyHelper.GetAllKey("services");

        public ServiceService(IRepositoryManager repositoryManager, IRedisService redisService, ILogger<ServiceService> logger)
        {
            _repositoryManager = repositoryManager;
            _redisService = redisService;
            _logger = logger;
        }

        public async Task<List<ServiceGetDto>> GetAllAsync(int skip = 0, int take = int.MaxValue)
        {
            var cached = await _redisService.GetAsync<List<ServiceGetDto>>(AllKey);
            if (cached != null)
                return cached.Skip(skip).Take(take).ToList();

            _logger.LogInformation("GetAllAsync - Redis boşdur, DB-yə sorğu göndərilir. skip: {Skip}, take: {Take}", skip, take);

            var data = await _repositoryManager.ServiceRepository.GetAllAsync(0, int.MaxValue);
            if (data == null || data.Count == 0)
                throw new NotFoundException("No service found.");

            var dtoList = data.MapToServiceGetDtos();

            await _redisService.SetAsync(AllKey, dtoList, _cacheExpiry);

            return dtoList.Skip(skip).Take(take).ToList();
        }

        public async Task<ServiceGetDto> GetByIdAsync(int id)
        {
            var cached = await _redisService.GetAsync<List<ServiceGetDto>>(AllKey);
            if (cached != null)
            {
                var item = cached.FirstOrDefault(x => x.Id == id);
                if (item != null)
                    return item;

                throw new NotFoundException($"Service with id {id} not found in cache.");
            }

            _logger.LogInformation("GetByIdAsync - Cache tapılmadı, DB-yə sorğu göndərilir. id: {Id}", id);

            var entity = await _repositoryManager.ServiceRepository.FindByIdAsync(id);
            if (entity == null)
                throw new NotFoundException($"Service with id {id} not found.");

            var dto = entity.MapToServiceGetDto();

            // Redis cache olmadığından, yeni list yaradıb cache-ə yazmaq olar (isteğe bağlı)
            await _redisService.SetAsync(AllKey, new List<ServiceGetDto> { dto }, _cacheExpiry);

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

            var cached = await _redisService.GetAsync<List<ServiceGetDto>>(AllKey) ?? new List<ServiceGetDto>();
            cached.Add(dto);
            await _redisService.SetAsync(AllKey, cached, _cacheExpiry);
        }

        public async Task UpdateAsync(ServiceUpdateDto updateDto)
        {
            var cached = await _redisService.GetAsync<List<ServiceGetDto>>(AllKey);
            if (cached == null)
                throw new NotFoundException("Service listesi cache-də tapılmadı.");

            var index = cached.FindIndex(x => x.Id == updateDto.Id);
            if (index == -1)
                throw new NotFoundException($"Service with id {updateDto.Id} not found");

            var category = await _repositoryManager.CategoryRepository.FindByIdAsync(updateDto.CategoryId);
            if (category == null)
                throw new NotFoundException($"Category with id {updateDto.CategoryId} not found");

            var entity = cached[index].MapToService();
            updateDto.MapToService(entity);
            await _repositoryManager.ServiceRepository.UpdateAsync(entity);

            cached[index] = entity.MapToServiceGetDto();
            await _redisService.SetAsync(AllKey, cached, _cacheExpiry);
        }

        public async Task DeleteAsync(int id)
        {
            var cached = await _redisService.GetAsync<List<ServiceGetDto>>(AllKey);
            if (cached == null)
                throw new NotFoundException("Service listesi cache-də tapılmadı.");

            var dto = cached.FirstOrDefault(x => x.Id == id);
            if (dto == null)
                throw new NotFoundException($"Service with id {id} not found");

            var entity = dto.MapToService();
            await _repositoryManager.ServiceRepository.DeleteAsync(entity);

            cached.RemoveAll(x => x.Id == id);
            await _redisService.SetAsync(AllKey, cached, _cacheExpiry);
        }
    }
}
