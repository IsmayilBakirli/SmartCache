using System.ComponentModel.DataAnnotations;

namespace SmartCache.Application.DTOs.Version
{
    public class VersionCheckRequestDto
    {
        [Required]
        public int CategoryVersion { get; set; }

        [Required]
        public int StoryVersion { get; set; }

        [Required]
        public int ServiceVersion { get; set; }
    }
}
