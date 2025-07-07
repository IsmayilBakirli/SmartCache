using System.ComponentModel.DataAnnotations;

namespace SmartCache.Application.DTOs.Category
{
    public class CategoryUpdateDto
    {
        [Required(ErrorMessage = "Id is required for update.")]
        public int Id { get; set; }

        [Required(ErrorMessage = "Name is required.")]
        [StringLength(100, ErrorMessage = "Name cannot be longer than 100 characters.")]
        public string Name { get; set; }

        public bool IsActive { get; set; }
    }
}
