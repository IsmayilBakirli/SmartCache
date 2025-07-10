using Microsoft.Data.SqlClient;
using SmartCache.Application.Contracts.Repositories.Contract.Base;
using SmartCache.Domain.Entities.Common;
using System.Data;
using System.Linq.Expressions;

namespace SmartCache.Persistence.Repositories.Base
{
    public class BaseRepository<T> : IBaseRepository<T> where T : BaseEntity, new()
    {
        protected readonly string _connectionString;
        protected readonly string _tableName;


        public BaseRepository(string connectionString, string tableName)
        {
            _connectionString = connectionString;
            _tableName = tableName;
        }


        protected virtual string TableName => _tableName;


        public virtual async Task<T> CreateAsync(T entity)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var properties = typeof(T).GetProperties()
                .Where(p => !string.Equals(p.Name, "Id", StringComparison.OrdinalIgnoreCase)
                         && (!p.PropertyType.IsClass || p.PropertyType == typeof(string)))
                .ToList();

            var columns = string.Join(", ", properties.Select(p => p.Name));
            var parameters = string.Join(", ", properties.Select(p => "@" + p.Name));

            using var command = connection.CreateCommand();
            command.CommandText = $@"
        INSERT INTO {TableName} ({columns}) 
        VALUES ({parameters}); 
        SELECT CAST(SCOPE_IDENTITY() AS INT);";

            foreach (var prop in properties)
            {
                object? value = prop.GetValue(entity);

                if (string.Equals(prop.Name, "CreatedDate", StringComparison.OrdinalIgnoreCase))
                {
                    if (value == null || (DateTime)value < new DateTime(1753, 1, 1))
                    {
                        value = DateTime.UtcNow.AddHours(4);
                        prop.SetValue(entity, value);
                    }
                }

                command.Parameters.AddWithValue("@" + prop.Name, value ?? DBNull.Value);
            }

            var result = await command.ExecuteScalarAsync();
            if (result != null)
            {
                var idProp = typeof(T).GetProperty("Id");
                if (idProp != null && idProp.CanWrite)
                {
                    idProp.SetValue(entity, Convert.ToInt32(result));
                }
            }

            return entity;
        }


        public virtual async Task UpdateAsync(T entity)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var properties = typeof(T).GetProperties()
                .Where(p => !string.Equals(p.Name, "Id", StringComparison.OrdinalIgnoreCase)
                         && (!p.PropertyType.IsClass || p.PropertyType == typeof(string)))
                .ToList();

            var updatedDateProp = typeof(T).GetProperty("UpdatedDate");
            if (updatedDateProp != null && updatedDateProp.CanWrite)
            {
                updatedDateProp.SetValue(entity, DateTime.UtcNow.AddHours(4));
                if (!properties.Any(p => p.Name == "UpdatedDate"))
                {
                    properties.Add(updatedDateProp);
                }
            }

            var setClause = string.Join(", ", properties.Select(p => $"{p.Name} = @{p.Name}"));

            using var command = connection.CreateCommand();
            command.CommandText = $"UPDATE {TableName} SET {setClause} WHERE Id = @Id";

            foreach (var prop in properties)
            {
                var value = prop.GetValue(entity) ?? DBNull.Value;
                command.Parameters.AddWithValue("@" + prop.Name, value);
            }

            var idProp = typeof(T).GetProperty("Id");
            if (idProp == null)
                throw new InvalidOperationException("Entity does not contain 'Id' property.");

            var idValue = idProp.GetValue(entity) ?? DBNull.Value;
            command.Parameters.AddWithValue("@Id", idValue);

            await command.ExecuteNonQueryAsync();
        }


        public virtual async Task DeleteAsync(T entity)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using var command = connection.CreateCommand();

            command.CommandText = $"UPDATE {TableName} SET IsDeleted = @IsDeleted WHERE Id = @Id";

            command.Parameters.AddWithValue("@IsDeleted", DateTime.UtcNow);
            command.Parameters.AddWithValue("@Id", entity.Id);

            await command.ExecuteNonQueryAsync();
        }


        public virtual async Task<T> FindByIdAsync(int id)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using var command = connection.CreateCommand();

            command.CommandText = $"SELECT * FROM {TableName} WHERE Id = @Id AND IsDeleted IS NULL";
            command.Parameters.AddWithValue("@Id", id);

            using var reader = await command.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                return MapToEntity(reader);
            }

            return null;
        }


        public virtual async Task<List<T>> GetAllAsync(int skip = 0, int take = int.MaxValue)
        {
            var list = new List<T>();

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using var command = connection.CreateCommand();

            command.CommandText = $@"
                SELECT * FROM {TableName} 
                WHERE IsDeleted IS NULL
                ORDER BY Id
                OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY";

            command.Parameters.AddWithValue("@Skip", skip);
            command.Parameters.AddWithValue("@Take", take);

            using var reader = await command.ExecuteReaderAsync();
            if (reader != null)
            {
                while (await reader.ReadAsync())
                {
                    list.Add(MapToEntity(reader));
                }
            }

            return list;
        }


        public virtual Task<List<T>> FindByConditionAsync(Expression<Func<T, bool>> expression, int skip = 0, int take = int.MaxValue)
        {
            throw new NotImplementedException("Override edin və öz SQL sorgunuzu yazın.");
        }


        protected virtual T MapToEntity(IDataRecord record)
        {
            var entity = new T();

            foreach (var prop in typeof(T).GetProperties())
            {
                if (prop.PropertyType.IsClass && prop.PropertyType != typeof(string))
                    continue;

                if (!ColumnExists(record, prop.Name))
                    continue;

                var value = record[prop.Name];
                if (value == DBNull.Value) value = null;

                prop.SetValue(entity, value);
            }

            return entity;
        }
        private bool ColumnExists(IDataRecord record, string columnName)
        {
            for (int i = 0; i < record.FieldCount; i++)
            {
                if (record.GetName(i).Equals(columnName, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }

    }
}
