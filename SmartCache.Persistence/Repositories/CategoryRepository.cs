using Microsoft.Data.SqlClient;
using SmartCache.Application.Contracts.Repositories.Contract;
using SmartCache.Domain.Entities;
using SmartCache.Persistence.Repositories.Base;
using System.Data;

namespace SmartCache.Persistence.Repositories
{
    public class CategoryRepository:BaseRepository<Category>,ICategoryRepository
    {
        public CategoryRepository(string connectionString):base(connectionString,"Categories") { }
        public async Task<bool> CanDeleteCategoryAsync(int categoryId)
        {
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand("CanDeleteCategory", connection)
            {
                CommandType = CommandType.StoredProcedure
            };
            command.Parameters.AddWithValue("@CategoryId", categoryId);

            await connection.OpenAsync();
            var count = (int)await command.ExecuteScalarAsync();

            return count == 0;
        }
    }
}
