using Microsoft.Extensions.DependencyInjection;
using SmartCache.Application.Contracts.Services.Contract;

namespace SmartCache.Persistence.Services
{
    public class ServiceManager : IServiceManager
    {
        private readonly Lazy<IServiceService> _serviceService;
        private readonly Lazy<ICategoryService> _categoryService;
        private readonly Lazy<IStoryService> _storyService;
        public ServiceManager(IServiceProvider serviceProvider)
        {
            _serviceService = new Lazy<IServiceService>(() => serviceProvider.GetRequiredService<IServiceService>());
            _categoryService = new Lazy<ICategoryService>(() => serviceProvider.GetRequiredService<ICategoryService>());
            _storyService = new Lazy<IStoryService>(() => serviceProvider.GetRequiredService<IStoryService>());
        }
        public IServiceService ServiceService => _serviceService.Value;
        public ICategoryService CategoryService => _categoryService.Value;
        public IStoryService StoryService => _storyService.Value;
    }
}
