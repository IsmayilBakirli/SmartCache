using Microsoft.Data.SqlClient;
using SmartCache.Application.Contracts.Repositories.Contract;
using SmartCache.Domain.Entities;
using SmartCache.Persistence.Repositories.Base;

namespace SmartCache.Persistence.Repositories
{
    public class ServiceRepository:BaseRepository<Service>,IServiceRepository
    {
        public ServiceRepository(string connectionString):base(connectionString,"Services") { }
        public override async Task<List<Service>> GetAllAsync(int skip = 0, int take = int.MaxValue)
        {
            var list = new List<Service>();

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using var command = connection.CreateCommand();

            command.CommandText = @"
        SELECT s.*, 
               c.Id AS CategoryId, 
               c.Name AS CategoryName, 
               c.IsActive AS CategoryIsActive
        FROM Services s
        LEFT JOIN Categories c ON s.CategoryId = c.Id AND c.IsDeleted IS NULL
        WHERE s.IsDeleted IS NULL
        ORDER BY s.Id
        OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY";

            command.Parameters.AddWithValue("@Skip", skip);
            command.Parameters.AddWithValue("@Take", take);

            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                var service = MapToEntity(reader);

                var hasCategory = !reader.IsDBNull(reader.GetOrdinal("CategoryId"));

                if (hasCategory)
                {
                    service.Category = new Category
                    {
                        Id = reader.GetInt32(reader.GetOrdinal("CategoryId")),
                        Name = reader.IsDBNull(reader.GetOrdinal("CategoryName"))
                            ? null
                            : reader.GetString(reader.GetOrdinal("CategoryName")),
                        IsActive = !reader.IsDBNull(reader.GetOrdinal("CategoryIsActive"))
                            && reader.GetBoolean(reader.GetOrdinal("CategoryIsActive"))
                    };
                }
                else
                {
                    service.Category = null;
                }

                list.Add(service);
            }

            return list;
        }



    }
}
