using SmartCache.Application.Common.Converters;
using System.Text.Json.Serialization;

namespace SmartCache.Application.DTOs.Service
{
    public class ServiceGetDto
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        public bool IsActive { get; set; }

        public decimal Price { get; set; }

        public int CategoryId { get; set; }
        public string CategoryName { get; set; }

        [JsonConverter(typeof(DateOnlyConverter<DateTime>))]
        public DateTime CreatedDate { get; set; }


        [JsonConverter(typeof(DateOnlyConverter<DateTime>))]
        public DateTime? UpdatedDate { get; set; }
    }
}
