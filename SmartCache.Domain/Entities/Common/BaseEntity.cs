namespace SmartCache.Domain.Entities.Common
{
    public class BaseEntity
    {
        public int Id { get; set; }
        public DateTime? IsDeleted { get; set; }
    }
}
