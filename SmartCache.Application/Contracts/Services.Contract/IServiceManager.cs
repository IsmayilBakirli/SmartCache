namespace SmartCache.Application.Contracts.Services.Contract
{
    public interface IServiceManager
    {
        public IServiceService ServiceService { get; }
        public ICategoryService CategoryService { get; }
        public IStoryService StoryService { get; }
        public ISyncService SyncService { get; }
    }
}
