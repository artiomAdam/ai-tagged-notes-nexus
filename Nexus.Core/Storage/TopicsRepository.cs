using Nexus.Core.Interfaces;
using Nexus.Core.Models;
using Nexus.Core.Utilities;
using System.Xml.Linq;

namespace Nexus.Core.Storage
{
    public class TopicsRepository : ITopicsRepository
    {
        private readonly DbContext _dbContext;
        private readonly string _table = "Topics";
        public TopicsRepository(DbContext dbContenxt)
        {
            _dbContext = dbContenxt;
        }
        public async Task DeleteAsync(string id)
        {
            using var conn = _dbContext.CreateConnection();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "DELETE FROM "+_table+" WHERE Id = $id";
            cmd.Parameters.AddWithValue("id", id);
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task<IEnumerable<Topic>> GetAllAsync()
        {
            var topics = new List<Topic>();
            using var conn = _dbContext.CreateConnection();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT Id, Name, Embedding FROM " + _table;
            using var reader = await cmd.ExecuteReaderAsync();
            while(await reader.ReadAsync())
            {
                topics.Add(new Topic
                {
                    Id = reader.GetString(0),
                    Name = reader.GetString(1),
                    Embedding = ByteUtils.BytesToFloatArray(reader["Embedding"]),
                });

            }
            return topics;
        }

        public async Task<Topic?> GetByIdAsync(string id)
        {
            using var conn = _dbContext.CreateConnection();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT Id, Name, Embedding FROM "+_table+" WHERE Id = $id";
            cmd.Parameters.AddWithValue("id", id);
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                return new Topic
                {
                    Id = reader.GetString(0),
                    Name = reader.GetString(1),
                    Embedding = ByteUtils.BytesToFloatArray(reader["Embedding"]),
                };
            }
            return null;
        }

        public async Task<string?> GetNameByIdAsync(string id)
        {
            using var conn = _dbContext.CreateConnection();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT Name FROM " + _table + " WHERE Id = $id";
            cmd.Parameters.AddWithValue("id", id);
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                return reader.GetString(0);
            }
            return null;
        }
        public async Task<string?> GetIdByNameAsync(string name)
        {
            using var conn = _dbContext.CreateConnection();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT Id FROM " + _table + " WHERE Name = $name";
            cmd.Parameters.AddWithValue("name", name);
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                return reader.GetString(0);
            }
            return null;
        }

        public async Task<Topic?> GetByNameAsync(string name)
        {
            using var conn = _dbContext.CreateConnection();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT Id, Name, Embedding FROM " + _table + " WHERE Name = $name";
            cmd.Parameters.AddWithValue("name", name);
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                return new Topic
                {
                    Id = reader.GetString(0),
                    Name = reader.GetString(1),
                    Embedding = ByteUtils.BytesToFloatArray(reader["Embedding"]),
                };
            }
            return null;
        }

        public async Task InsertAsync(Topic topic)
        {
            using var conn = _dbContext.CreateConnection();
            var cmd = conn.CreateCommand();
            cmd.CommandText = @"INSERT INTO " + _table + @" (Id, Name, Embedding)
                                VALUES ($id, $name, $embedding)
                                ON CONFLICT(Name) DO NOTHING;";
            cmd.Parameters.AddWithValue("id", topic.Id);
            cmd.Parameters.AddWithValue("name", topic.Name);
            cmd.Parameters.AddWithValue("embedding", (object?)ByteUtils.FloatArrayToBytes(topic.Embedding) ?? DBNull.Value);

            await cmd.ExecuteNonQueryAsync();
        }

        public async Task UpdateAsync(Topic topic)
        {
            using var conn = _dbContext.CreateConnection();
            var cmd = conn.CreateCommand();
            cmd.CommandText = @" 
                UPDATE " + _table + @"
                SET Name = $name, Embedding = $embedding
                WHERE Id = $id";
            cmd.Parameters.AddWithValue("id", topic.Id);
            cmd.Parameters.AddWithValue("name", topic.Name);
            cmd.Parameters.AddWithValue("embedding", (object?)ByteUtils.FloatArrayToBytes(topic.Embedding) ?? DBNull.Value);

            await cmd.ExecuteNonQueryAsync();
        }

        public async Task UpdateEmbeddingAsync(string id, float[] embedding)
        {
            using var conn = _dbContext.CreateConnection();
            var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                    UPDATE " + _table + @"
                    SET Embedding = $embedding
                    WHERE Id = $id";
            cmd.Parameters.AddWithValue("id", id);
            cmd.Parameters.AddWithValue("embedding", (object?)ByteUtils.FloatArrayToBytes(embedding) ?? DBNull.Value);

            await cmd.ExecuteNonQueryAsync();
        }
    }
}
