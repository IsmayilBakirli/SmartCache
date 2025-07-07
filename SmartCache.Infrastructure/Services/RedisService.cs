using SmartCache.Application.Common.Interfaces;
using StackExchange.Redis;
using System.Text.Json;
using System.Linq;
using System.Threading.Tasks;
using System;

public class RedisService : IRedisService
{
    private readonly IDatabase _db;
    private readonly IConnectionMultiplexer _redis;

    public RedisService(IConnectionMultiplexer redis)
    {
        _redis = redis;
        _db = redis.GetDatabase();
    }

    public async Task<T?> GetAsync<T>(string key)
    {
        var value = await _db.StringGetAsync(key);
        if (value.IsNullOrEmpty) return default;

        return JsonSerializer.Deserialize<T>(value!);
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null)
    {
        var json = JsonSerializer.Serialize(value);
        await _db.StringSetAsync(key, json, expiry);
    }

    public async Task RemoveAsync(string key)
    {
        await _db.KeyDeleteAsync(key);
    }

    public async Task RemoveByPatternAsync(string pattern)
    {
        var server = GetServer();

        // "pattern" misal üçün "stories:all:*"
        var keys = server.Keys(pattern: pattern).ToArray();

        foreach (var key in keys)
        {
            await _db.KeyDeleteAsync(key);
        }
    }

    private IServer GetServer()
    {
        var endpoint = _redis.GetEndPoints().First();
        return _redis.GetServer(endpoint);
    }
}
