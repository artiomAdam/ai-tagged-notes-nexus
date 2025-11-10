
using Nexus.Core.Interfaces;
using Nexus.Core.Models;

namespace Nexus.Core.Storage
{
    public class NoteRepository : INoteRepository
    {
        private readonly DbContext _dbContext;
        private readonly string _table = "Notes";
        public NoteRepository(DbContext dbContext)
        {
            _dbContext = dbContext;
        }
        public async Task DeleteAsync(string id)
        {
            using var conn = _dbContext.CreateConnection();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "DELETE FROM "+_table+" WHERE Id = $id";
            cmd.Parameters.AddWithValue("id", id);
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task<IEnumerable<Note>> GetAllAsync()
        {
            var notes = new List<Note>();
            using var conn = _dbContext.CreateConnection();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT Id, Title, Content, CreatedAt, UpdatedAt, ParentId FROM "+_table;
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                notes.Add(new Note
                {
                    Id = reader.GetString(0),
                    Title = reader.GetString(1),
                    Content = reader.GetString(2),
                    CreatedAt = DateTime.Parse(reader.GetString(3)),
                    UpdatedAt = DateTime.Parse(reader.GetString(4)),
                    ParentId = reader.IsDBNull(5) ? null : reader.GetString(5),
                });
            }
            return notes;
        }

        public async Task<Note?> GetByIdAsync(string id)
        {
            using var conn = _dbContext.CreateConnection();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT Id, Title, Content, CreatedAt, UpdatedAt, ParentId FROM "+_table+" WHERE Id = $id";
            cmd.Parameters.AddWithValue("id", id);

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                return new Note
                {
                    Id = reader.GetString(0),
                    Title = reader.GetString(1),
                    Content = reader.GetString(2),
                    CreatedAt = DateTime.Parse(reader.GetString(3)),
                    UpdatedAt = DateTime.Parse(reader.GetString(4)),
                    ParentId = reader.IsDBNull(5) ? null : reader.GetString(5),
                };
            }
            return null;

        }

        public async Task InsertAsync(Note note)
        {
            using var conn = _dbContext.CreateConnection();
            var cmd = conn.CreateCommand();
            cmd.CommandText = @" 
                INSERT INTO "+_table+@" (Id, Title, Content, CreatedAt, UpdatedAt, ParentId)
                VALUES ($id, $title, $content, $created, $updated, $parentId)";
            cmd.Parameters.AddWithValue("id", note.Id);
            cmd.Parameters.AddWithValue("title", note.Title);
            cmd.Parameters.AddWithValue("content", note.Content);
            cmd.Parameters.AddWithValue("created", note.CreatedAt);
            cmd.Parameters.AddWithValue("updated", note.UpdatedAt);
            cmd.Parameters.AddWithValue("parentId", (object?)note.ParentId ?? DBNull.Value);

            await cmd.ExecuteNonQueryAsync();
        }

        public async Task UpdateAsync(Note note)
        {
            using var conn = _dbContext.CreateConnection();
            var cmd = conn.CreateCommand();
            cmd.CommandText = @" 
                UPDATE "+_table+@"
                SET Title = $title, Content = $content, UpdatedAt = $updated
                WHERE Id = $id";
            cmd.Parameters.AddWithValue("id", note.Id);
            cmd.Parameters.AddWithValue("title", note.Title);
            cmd.Parameters.AddWithValue("content", note.Content);
            DateTime now = DateTime.UtcNow;
            note.UpdatedAt = now;
            cmd.Parameters.AddWithValue("updated", now);

            await cmd.ExecuteNonQueryAsync();
        }
    }
}
