using SmartCache.Domain.Entities.Common;

namespace SmartCache.Domain.Entities
{
    public class Service:BaseEntity,IHasCreatedDate,IHasUpdatedDate
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public bool IsActive { get; set; }
        public decimal Price { get; set; }

        public int CategoryId { get; set; }
        public Category Category { get; set; }  

        public DateTime CreatedDate { get; set; }
        public DateTime? UpdatedDate { get; set; }
    }
}
