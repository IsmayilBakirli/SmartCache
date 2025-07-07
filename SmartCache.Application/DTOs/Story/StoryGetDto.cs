using SmartCache.Application.Common.Converters;
using System.Text.Json.Serialization;

namespace SmartCache.Application.DTOs.Story
{
    public class StoryGetDto
    {
        public int Id { get; set; }

        public string Title { get; set; }

        public string Content { get; set; }

        public string ImageUrl { get; set; }

        public bool IsPublished { get; set; }

        [JsonConverter(typeof(DateOnlyConverter<DateTime>))]

        public DateTime CreatedDate { get; set; }

        [JsonConverter(typeof(DateOnlyConverter<DateTime>))]
        public DateTime? UpdatedDate { get; set; }
    }
}
