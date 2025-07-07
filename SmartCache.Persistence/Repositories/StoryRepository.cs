using SmartCache.Application.Contracts.Repositories.Contract;
using SmartCache.Domain.Entities;
using SmartCache.Persistence.Repositories.Base;

namespace SmartCache.Persistence.Repositories
{
    public class StoryRepository:BaseRepository<Story>,IStoryRepository
    {
        public StoryRepository(string connectionString) : base(connectionString, "Stories") { }

    }
}
