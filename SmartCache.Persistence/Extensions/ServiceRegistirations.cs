using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SmartCache.Application.Common.Helpers;
using SmartCache.Application.Common.Interfaces;
using SmartCache.Application.Contracts.Repositories.Contract;
using SmartCache.Application.Contracts.Services.Contract;
using SmartCache.Persistence.Contexts;
using SmartCache.Persistence.Repositories;
using SmartCache.Persistence.Services;
namespace SmartCache.Persistence.Extensions
{
    public static class ServiceRegistiration
    {
        public static IServiceCollection AddPersistenceServices(this IServiceCollection serviceCollection, IConfiguration configuration)
        {
            serviceCollection.AddScoped<IRepositoryManager, RepositoryManager>();
            serviceCollection.AddScoped<IServiceManager, ServiceManager>();

            serviceCollection.AddScoped<IServiceRepository,ServiceRepository>();
            serviceCollection.AddScoped<IServiceService, ServiceService>();

            serviceCollection.AddScoped<ICategoryRepository,CategoryRepository>();
            serviceCollection.AddScoped<ICategoryService, CategoryService>();

            serviceCollection.AddScoped<IStoryRepository, StoryRepository>();
            serviceCollection.AddScoped<IStoryService, StoryService>();

            serviceCollection.AddScoped<ISyncService, SyncService>();

            serviceCollection.AddSingleton(configuration.GetConnectionString("DefaultConnection"));
            serviceCollection.AddDbContext<SmartCacheContext>(options =>
            {
                options.UseSqlServer(configuration.GetConnectionString("DefaultConnection"),
                    d => d.MigrationsAssembly("SmartCache.Persistence"));
            });
            return serviceCollection;
        }
    }
}
