using SmartCache.Application.DTOs.Service;
using SmartCache.Domain.Entities;

namespace SmartCache.Application.MappingProfile
{
    public static class ServiceMapping
    {
        public static ServiceGetDto MapToServiceGetDto(this Service service)
        {
            return new ServiceGetDto
            {
                Id = service.Id,
                Name = service.Name,
                Description = service.Description,
                IsActive = service.IsActive,
                Price = service.Price,
                CategoryId = service.CategoryId,
                CategoryName = service.Category?.Name,  
                CreatedDate = service.CreatedDate,
                UpdatedDate = service.UpdatedDate
            };
        }


        public static List<ServiceGetDto> MapToServiceGetDtos(this List<Service> services)
        {
            return services.Select(service => service.MapToServiceGetDto()).ToList();
        }
        public static Service MapToService(this ServiceCreateDto createDto)
        {
            return new Service
            {
                Name = createDto.Name,
                Description = createDto.Description,
                IsActive = createDto.IsActive,
                Price = createDto.Price,
                CategoryId = createDto.CategoryId
            };
        }
        public static void MapToService(this ServiceUpdateDto updateDto, Service service)
        {
            service.Name = updateDto.Name;
            service.Description = updateDto.Description;
            service.IsActive = updateDto.IsActive;
            service.Price = updateDto.Price;
            service.CategoryId = updateDto.CategoryId;
        }
        public static Service MapToService(this ServiceGetDto dto)
        {
            return new Service
            {
                Id = dto.Id,
                Name = dto.Name,
                Description = dto.Description,
                IsActive = dto.IsActive,
                Price = dto.Price,
                CategoryId = dto.CategoryId,
                CreatedDate = dto.CreatedDate,
                UpdatedDate = dto.UpdatedDate
            };
        }
    }
}
