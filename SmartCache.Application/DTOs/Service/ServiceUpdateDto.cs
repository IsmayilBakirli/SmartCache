using System.ComponentModel.DataAnnotations;

namespace SmartCache.Application.DTOs.Service
{
    public class ServiceUpdateDto
    {
        [Required(ErrorMessage = "Id is required for update.")]
        public int Id { get; set; }

        [Required(ErrorMessage = "Name is required.")]
        [StringLength(200, ErrorMessage = "Name cannot be longer than 200 characters.")]
        public string Name { get; set; }

        [StringLength(200, ErrorMessage = "Description cannot be longer than 200 characters.")]
        public string Description { get; set; }

        public bool IsActive { get; set; }

        [Required(ErrorMessage = "Price is required.")]
        [Range(0.0, Double.MaxValue, ErrorMessage = "Price must be positive.")]
        public decimal Price { get; set; }

        [Required(ErrorMessage = "CategoryId is required.")]
        public int CategoryId { get; set; }
    }
}
