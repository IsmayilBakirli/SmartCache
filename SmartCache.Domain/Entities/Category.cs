using SmartCache.Domain.Entities.Common;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartCache.Domain.Entities
{
    public class Category:BaseEntity,IHasCreatedDate,IHasUpdatedDate
    {
        public string Name { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? UpdatedDate { get; set; }
    }
}
