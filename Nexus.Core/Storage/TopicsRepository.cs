using Nexus.Core.Interfaces;
using Nexus.Core.Models;
using Nexus.Core.Utilities;
using System.Xml.Linq;

namespace Nexus.Core.Storage
{
    public class TopicsRepository : ITopicsRepository
    {
        private readonly DbContext _dbContext;
        private Dictionary<string, Topic> _cache = new();
        private bool _cacheLoaded = false;
        private readonly object _cacheLock = new();
        private readonly string _table = "Topics";
        public TopicsRepository(DbContext dbContenxt)
        {
            _dbContext = dbContenxt;
        }

        private async Task EnsureCacheLoadedAsync()
        {
            if (_cacheLoaded)
                return;

            lock (_cacheLock)
            {
                if (_cacheLoaded) return;
            }

            var newCache = new Dictionary<string, Topic>();
            using var conn = _dbContext.CreateConnection();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT Id, Name, Embedding FROM " + _table;

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var topic = new Topic
                {
                    Id = reader.GetString(0),
                    Name = reader.GetString(1),
                    Embedding = ByteUtils.BytesToFloatArray(reader["Embedding"])
                };

                newCache[topic.Id] = topic;
            }

            lock (_cacheLock)
            {
                _cache = newCache;
                _cacheLoaded = true;
            }
        }


        public async Task<IEnumerable<Topic>> GetAllAsync()
        {
            await EnsureCacheLoadedAsync();
            return _cache.Values;
        }

        public async Task<Topic?> GetByIdAsync(string id)
        {
            await EnsureCacheLoadedAsync();
            return _cache.TryGetValue(id, out var topic)
                ? topic : null;
        }

        public async Task<string?> GetNameByIdAsync(string id)
        {
            await EnsureCacheLoadedAsync();
            return _cache.Values.FirstOrDefault(t => t.Id == id)?.Name;
        }
        public async Task<string?> GetIdByNameAsync(string name)
        {
            await EnsureCacheLoadedAsync();
            return _cache.Values.FirstOrDefault(t => t.Name == name)?.Id;
        }

        public async Task<Topic?> GetByNameAsync(string name)
        {
            await EnsureCacheLoadedAsync();
            return _cache.Values.FirstOrDefault(t => t.Name == name);
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

            await EnsureCacheLoadedAsync();
            lock(_cacheLock)
            {
                _cache[topic.Id] = topic;
            }
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

            await EnsureCacheLoadedAsync();
            lock (_cacheLock)
            {
                _cache[topic.Id] = topic;
            }
        }
        public async Task DeleteAsync(string id)
        {
            using var conn = _dbContext.CreateConnection();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "DELETE FROM " + _table + " WHERE Id = $id";
            cmd.Parameters.AddWithValue("id", id);
            await cmd.ExecuteNonQueryAsync();

            await EnsureCacheLoadedAsync();
            lock (_cacheLock)
            {
                _cache.Remove(id);
            }
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

            await EnsureCacheLoadedAsync();
            lock (_cacheLock)
            {
                if (_cache.TryGetValue(id, out var topic))
                    topic.Embedding = embedding;
            }
        }
    }
}
