using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartCache.Application.Contracts.Repositories.Contract
{
    public interface IRepositoryManager
    {
        public IServiceRepository ServiceRepository { get; }
        public ICategoryRepository CategoryRepository { get; }
        public IStoryRepository StoryRepository { get; }
    }
}
