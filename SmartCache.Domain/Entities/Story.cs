using SmartCache.Domain.Entities.Common;

namespace SmartCache.Domain.Entities
{
    public class Story:BaseEntity,IHasCreatedDate,IHasUpdatedDate
    {
        public string Title { get; set; }
        public string Content { get; set; }
        public string ImageUrl { get; set; }
        public bool IsPublished { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? UpdatedDate { get; set; }
    }
}
