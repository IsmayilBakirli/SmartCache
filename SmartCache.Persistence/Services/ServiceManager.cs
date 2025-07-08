using Microsoft.Extensions.DependencyInjection;
using SmartCache.Application.Contracts.Services.Contract;

namespace SmartCache.Persistence.Services
{
    public class ServiceManager : IServiceManager
    {
        private readonly Lazy<IServiceService> _serviceService;
        private readonly Lazy<ICategoryService> _categoryService;
        private readonly Lazy<IStoryService> _storyService;
        private readonly Lazy<ISyncService> _syncService;
        public ServiceManager(IServiceProvider serviceProvider)
        {
            _serviceService = new Lazy<IServiceService>(() => serviceProvider.GetRequiredService<IServiceService>());
            _categoryService = new Lazy<ICategoryService>(() => serviceProvider.GetRequiredService<ICategoryService>());
            _storyService = new Lazy<IStoryService>(() => serviceProvider.GetRequiredService<IStoryService>());
            _syncService = new Lazy<ISyncService>(() => serviceProvider.GetRequiredService<ISyncService>());
        }
        public IServiceService ServiceService => _serviceService.Value;
        public ICategoryService CategoryService => _categoryService.Value;
        public IStoryService StoryService => _storyService.Value;
        public ISyncService SyncService => _syncService.Value;

    }
}
