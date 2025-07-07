using SmartCache.Application.DTOs.Category;
using SmartCache.Domain.Entities;

namespace SmartCache.Application.MappingProfile
{
    public static class CategoryMapping
    {
        public static CategoryGetDto MapToCategoryGetDto(this Category category)
        {
            return new CategoryGetDto
            {
                Id = category.Id,
                Name = category.Name,
                IsActive = category.IsActive,
                CreatedDate = category.CreatedDate,
                UpdatedDate = category.UpdatedDate
            };
        }

        public static List<CategoryGetDto> MapToCategoryGetDtos(this List<Category> categories)
        {
            return categories.Select(category => category.MapToCategoryGetDto()).ToList();
        }
        public static Category MapToCategory(this CategoryCreateDto createDto)
        {
            return new Category
            {
                Name = createDto.Name,
                IsActive = createDto.IsActive
            };
        }

        public static void MapToCategory(this CategoryUpdateDto updateDto, Category category)
        {
            category.Name = updateDto.Name;
            category.IsActive = updateDto.IsActive;
        }
        public static Category MapToCategory(this CategoryGetDto dto)
        {
            return new Category
            {
                Id = dto.Id,
                Name = dto.Name,
                IsActive = dto.IsActive,
                CreatedDate = dto.CreatedDate,
                UpdatedDate = dto.UpdatedDate
            };
        }
    }
}
