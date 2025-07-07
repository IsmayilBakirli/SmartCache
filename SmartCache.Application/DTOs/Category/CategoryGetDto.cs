using SmartCache.Application.Common.Converters;
using System.Text.Json.Serialization;

namespace SmartCache.Application.DTOs.Category
{
    public class CategoryGetDto
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public bool IsActive { get; set; }

        [JsonConverter(typeof(DateOnlyConverter<DateTime>))]
        public DateTime CreatedDate { get; set; }

        [JsonConverter(typeof(DateOnlyConverter<DateTime>))]

        public DateTime? UpdatedDate { get; set; }

        //[NotMapped]
        //public ICollection<ServiceGetDto> Services { get; set; } = new List<ServiceGetDto>();
    }
}
