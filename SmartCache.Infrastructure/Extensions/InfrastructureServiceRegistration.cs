using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SmartCache.Application.Common.Interfaces;
using StackExchange.Redis;
namespace SmartCache.Infrastructure.Extensions
{
    public static class InfrastructureServiceRegistration
    {
        public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
        {
            var redisConnection = configuration.GetSection("Redis")["ConnectionString"];

            services.AddSingleton<IConnectionMultiplexer>(sp =>
                ConnectionMultiplexer.Connect(redisConnection));

            services.AddSingleton<IRedisService, RedisService>();

            return services;
        }
    }
}
