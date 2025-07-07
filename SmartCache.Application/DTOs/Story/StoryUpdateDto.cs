using System.ComponentModel.DataAnnotations;

namespace SmartCache.Application.DTOs.Story
{
    public class StoryUpdateDto
    {
        [Required(ErrorMessage = "Id is required.")]
        public int Id { get; set; }

        [Required(ErrorMessage = "Title is required.")]
        [StringLength(100, ErrorMessage = "Title cannot be longer than 100 characters.")]
        public string Title { get; set; }

        [Required(ErrorMessage = "Content is required.")]
        public string Content { get; set; }

        [StringLength(500, ErrorMessage = "ImageUrl cannot be longer than 500 characters.")]
        public string ImageUrl { get; set; }

        public bool IsPublished { get; set; }
    }
}
