using Microsoft.Data.SqlClient;
using SmartCache.Application.Contracts.Repositories.Contract;
using SmartCache.Domain.Entities;
using SmartCache.Persistence.Repositories.Base;

namespace SmartCache.Persistence.Repositories
{
    public class CategoryRepository:BaseRepository<Category>,ICategoryRepository
    {
        public CategoryRepository(string connectionString):base(connectionString,"Categories") { }
        public async Task<bool> CanDeleteCategoryAsync(int categoryId)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using var command = connection.CreateCommand();
            command.CommandText = "SELECT COUNT(1) FROM Services WHERE CategoryId = @CategoryId AND IsDeleted IS NULL";
            command.Parameters.AddWithValue("@CategoryId", categoryId);

            var count = (int)await command.ExecuteScalarAsync();

            return count == 0;  // 0-dırsa silmək olar, yoxsa olmaz
        }
        

    }
}
