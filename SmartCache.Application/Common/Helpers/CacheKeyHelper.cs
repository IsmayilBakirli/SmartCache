namespace SmartCache.Application.Common.Helpers
{
    public static class CacheKeyHelper
    {
        // Əvvəlcədən hazırlanmış key builder-lər
        public static readonly EntityKeyBuilder Categories = For("categories");
        public static readonly EntityKeyBuilder Services = For("services");
        public static readonly EntityKeyBuilder Stories = For("stories");

        // Əgər yeni entity əlavə etsən:
        // public static readonly EntityKeyBuilder Users = For("users");

        // Dynamic key builder
        public static EntityKeyBuilder For(string entityName)
            => new EntityKeyBuilder(entityName);

        // Nested builder sinifi
        public class EntityKeyBuilder
        {
            private readonly string _entity;

            public EntityKeyBuilder(string entityName)
            {
                _entity = entityName.ToLowerInvariant();
            }

            public string All => _entity;
            public string Version => $"{_entity}:version";
            public string Init => $"{_entity}:all:initialized";
            public string Detail(int id) => $"{_entity}:detailcache:{id}";
        }
    }
}
