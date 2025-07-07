namespace SmartCache.Application.Common.Helpers
{
    public static class CacheKeyHelper
    {
        public static string GetAllKey(string entityName)
            => $"{entityName.ToLowerInvariant()}";

        public static string GetDetailKey(string entityName, int id)
            => $"{entityName.ToLowerInvariant()}:detailcache:{id}";

        public static string GetInitKey(string entityName)
            => $"{entityName.ToLowerInvariant()}:all:initialized";
    }
}
