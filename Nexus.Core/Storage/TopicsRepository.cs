using Nexus.Core.Interfaces;
using Nexus.Core.Models;

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
            cmd.CommandText = "SELECT Id, Name FROM " + _table;
            using var reader = await cmd.ExecuteReaderAsync();
            while(await reader.ReadAsync())
            {
                topics.Add(new Topic
                {
                    Id = reader.GetString(0),
                    Name = reader.GetString(1)
                });

            }
            return topics;
        }

        public async Task<Topic?> GetByIdAsync(string id)
        {
            using var conn = _dbContext.CreateConnection();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT Id, Name FROM "+_table+" WHERE Id = $id";
            cmd.Parameters.AddWithValue("id", id);
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                return new Topic
                {
                    Id = reader.GetString(0),
                    Name = reader.GetString(1)
                };
            }
            return null;
        }

        public async Task<Topic?> GetByNameAsync(string name)
        {
            using var conn = _dbContext.CreateConnection();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT Id, Name FROM " + _table + " WHERE Name = $name";
            cmd.Parameters.AddWithValue("name", name);
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                return new Topic
                {
                    Id = reader.GetString(0),
                    Name = reader.GetString(1)
                };
            }
            return null;
        }

        public async Task InsertAsync(Topic topic)
        {
            using var conn = _dbContext.CreateConnection();
            var cmd = conn.CreateCommand();
            cmd.CommandText = @"INSERT INTO "+_table+ @" (Id, Name)
                                VALUES ($id, $name)
                                ON CONFLICT(Name) DO NOTHING;";
            cmd.Parameters.AddWithValue("id", topic.Id);
            cmd.Parameters.AddWithValue("name", topic.Name);

            await cmd.ExecuteNonQueryAsync();
        }

        public async Task UpdateAsync(Topic topic)
        {
            using var conn = _dbContext.CreateConnection();
            var cmd = conn.CreateCommand();
            cmd.CommandText = @" 
                UPDATE " + _table + @"
                SET Name = $name
                WHERE Id = $id
                ON CONFLICT(Name) DO NOTHING;";
            cmd.Parameters.AddWithValue("id", topic.Id);
            cmd.Parameters.AddWithValue("name", topic.Name);

            await cmd.ExecuteNonQueryAsync();
        }
    }
}
