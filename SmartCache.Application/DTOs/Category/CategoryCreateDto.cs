using System.ComponentModel.DataAnnotations;

namespace SmartCache.Application.DTOs.Category
{
    public class CategoryCreateDto
    {
        [Required(ErrorMessage = "Name is required.")]
        [StringLength(100, ErrorMessage = "Name can be at most 100 characters long.")]
        public string Name { get; set; }

        public bool IsActive { get; set; } = true;

    }
}
