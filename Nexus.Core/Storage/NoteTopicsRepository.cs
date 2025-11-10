using Nexus.Core.Interfaces;
using Nexus.Core.Models;

namespace Nexus.Core.Storage
{
    public class NoteTopicsRepository : INoteTopicsRepository
    {
        private readonly DbContext _dbContext;
        private readonly string _table = "NoteTopics";
        public NoteTopicsRepository(DbContext dbContext) 
        {
            _dbContext = dbContext;
        }
        public async Task AddTopicToNoteAsync(string noteId, string topicId)
        {
            using var conn = _dbContext.CreateConnection();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "INSERT OR IGNORE INTO "+_table+" (NoteId, TopicId) VALUES ($noteId, $topicId)";
            cmd.Parameters.AddWithValue("$noteId", noteId);
            cmd.Parameters.AddWithValue("topicId", topicId);
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task DeleteByNoteIdAsync(string noteId)
        {
            using var conn = _dbContext.CreateConnection();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "DELETE FROM " + _table + " WHERE NoteId = $noteId";
            cmd.Parameters.AddWithValue("noteId", noteId);
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task DeleteByTopicIdAsync(string topicId)
        {
            using var conn = _dbContext.CreateConnection();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "DELETE FROM " + _table + " WHERE TopicId = $topicId";
            cmd.Parameters.AddWithValue("topicId", topicId);
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task<IEnumerable<string>> GetNotesForTopicAsync(string topicId)
        {
            var notes = new List<string>();
            using var conn = _dbContext.CreateConnection();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT NoteId FROM " + _table + " WHERE TopicId = $topicId";
            cmd.Parameters.AddWithValue("topicId", topicId);
            using var reader = await cmd.ExecuteReaderAsync();
            while(await reader.ReadAsync())
            {
                notes.Add(reader.GetString(0));
            }
            return notes;
        }

        public async Task<IEnumerable<string>> GetTopicsForNoteAsync(string noteId)
        {
            var topics = new List<string>();
            using var conn = _dbContext.CreateConnection();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT TopicId FROM " + _table + " WHERE NoteId = $noteId";
            cmd.Parameters.AddWithValue("noteId", noteId);
            using var reader = await cmd.ExecuteReaderAsync();
            while(await reader.ReadAsync())
            {
                topics.Add(reader.GetString(0));
            }
            return topics;
        }

        public async Task RemoveTopicFromNoteAsync(string noteId, string topicId)
        {
            using var conn = _dbContext.CreateConnection();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "DELETE FROM " + _table + " WHERE NoteId = $noteId AND TopicId = $topicId";
            cmd.Parameters.AddWithValue("noteId", noteId);
            cmd.Parameters.AddWithValue("topicId", topicId);
            await cmd.ExecuteNonQueryAsync();
        }
    }
}
