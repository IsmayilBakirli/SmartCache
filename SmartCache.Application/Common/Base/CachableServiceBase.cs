using Microsoft.Extensions.Logging;
using SmartCache.Application.Common.Interfaces;

public abstract class CachableServiceBase<TGetDto>
{
    protected readonly IRedisService _redisService;
    protected readonly ILogger _logger;
    protected readonly TimeSpan _cacheExpiry = TimeSpan.FromMinutes(10);

    protected CachableServiceBase(IRedisService redisService, ILogger logger)
    {
        _redisService = redisService;
        _logger = logger;
    }

    protected async Task<T?> GetCacheAsync<T>(string key)
    {
        var cached = await _redisService.GetAsync<T>(key);

        _logger.LogInformation(cached != null
            ? "Cache hit for key: {Key}"
            : "Cache miss for key: {Key}", key);

        return cached;
    }

    protected async Task SetCacheAsync<T>(string key, T value)
    {
        await _redisService.SetAsync(key, value, _cacheExpiry);
        _logger.LogInformation("Cache set for key: {Key}", key);
    }

    protected async Task RemoveCacheAsync(string key)
    {
        await _redisService.RemoveAsync(key);
        _logger.LogInformation("Cache removed for key: {Key}", key);
    }

    protected async Task<T> GetOrSetCacheAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null)
    {
        var cached = await _redisService.GetAsync<T>(key);
        if (cached != null)
        {
            _logger.LogInformation("Cache hit for key: {Key}", key);
            return cached;
        }

        _logger.LogInformation("Cache miss for key: {Key}. Loading from DB...",key);
        var result = await factory();
        await _redisService.SetAsync(key, result, expiration ?? _cacheExpiry);
        _logger.LogInformation("Data cached for key: {Key}", key);
        return result;
    }

    protected async Task<int> GetVersionAsync(string versionKey)
    {
        var version = await _redisService.GetAsync<int?>(versionKey) ?? 0;

        if (version == 0)
            await _redisService.SetAsync(versionKey, 0);

        _logger.LogInformation(version == 0
            ? "Version initialized to 0."
            : "Current version: {Version}", version);

        return version;
    }

    protected async Task IncreaseVersionAsync(string versionKey)
    {
        var version = await GetVersionAsync(versionKey);
        version++;
        await _redisService.SetAsync(versionKey, version);
        _logger.LogInformation("Version increased to: {Version}", version);
    }

    protected async Task<bool> CheckVersionChange(string versionKey, int clientVersion)
    {
        var currentVersion = await GetVersionAsync(versionKey);
        var hasChanged = clientVersion != currentVersion;

        if (!hasChanged)
            _logger.LogInformation("No version change detected. Client version: {ClientVersion}", clientVersion);

        return hasChanged;
    }

    protected async Task UpdateCacheAfterCreateAsync(string detailKey, string allKey, string versionKey, TGetDto dto)
    {
        await SetCacheAsync(detailKey, dto);

        var existingList = await GetCacheAsync<List<TGetDto>>(allKey);
        if (existingList != null)
        {
            existingList.Add(dto);
            await SetCacheAsync(allKey, existingList);
            _logger.LogInformation("Created DTO added to all cache list.");
        }

        await IncreaseVersionAsync(versionKey);
    }

    protected async Task UpdateCacheAfterUpdateAsync(string detailKey, string allKey, string versionKey, TGetDto dto)
    {
        await SetCacheAsync(detailKey, dto);
        await RemoveCacheAsync(allKey);
        _logger.LogInformation("Updated DTO cached, all cache cleared.");
        await IncreaseVersionAsync(versionKey);
    }

    protected async Task UpdateCacheAfterDeleteAsync(string detailKey, string allKey, string versionKey)
    {
        await RemoveCacheAsync(detailKey);
        await RemoveCacheAsync(allKey);
        _logger.LogInformation("Deleted keys removed from cache.");
        await IncreaseVersionAsync(versionKey);
    }
}
