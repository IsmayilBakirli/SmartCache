using Microsoft.Extensions.DependencyInjection;
using SmartCache.Application.Contracts.Repositories.Contract;

namespace SmartCache.Persistence.Repositories
{
    public class RepositoryManager : IRepositoryManager
    {
        private readonly Lazy<IServiceRepository> _serviceRepository;
        private readonly Lazy<ICategoryRepository> _categoryRepository;
        private readonly Lazy<IStoryRepository> _storyRepository;

        public RepositoryManager(IServiceProvider serviceProvider)
        {
            _serviceRepository = new Lazy<IServiceRepository>(serviceProvider.GetRequiredService<IServiceRepository>());
            _categoryRepository = new Lazy<ICategoryRepository>(serviceProvider.GetRequiredService<ICategoryRepository>());
            _storyRepository = new Lazy<IStoryRepository>(serviceProvider.GetRequiredService<IStoryRepository>());
        }

        public IServiceRepository ServiceRepository => _serviceRepository.Value;
        public ICategoryRepository CategoryRepository => _categoryRepository.Value;
        public IStoryRepository StoryRepository => _storyRepository.Value;
    }
}
