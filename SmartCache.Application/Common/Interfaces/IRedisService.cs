namespace SmartCache.Application.Common.Interfaces
{
    public interface IRedisService
    {
        Task<T?> GetAsync<T>(string key);
        Task SetAsync<T>(string key, T value, TimeSpan? expiry = null);
        Task RemoveAsync(string key);
        Task RemoveByPatternAsync(string pattern);
    }

}
