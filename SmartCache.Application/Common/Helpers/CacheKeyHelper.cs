namespace SmartCache.Application.Common.Helpers
{
    public static class CacheKeyHelper
    {
        public static string GetAllKey(string entityName, int skip = 0, int take = int.MaxValue)
            => $"{entityName.ToLowerInvariant()}:all:{skip}:{take}";

        public static string GetDetailKey(string entityName, int id)
            => $"{entityName.ToLowerInvariant()}:detailcache:{id}";

        public static string GetInitKey(string entityName)
            => $"{entityName.ToLowerInvariant()}:all:initialized";
    }
}
