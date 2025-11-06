
using Nexus.Core.Interfaces;
using Nexus.Core.Models;

namespace Nexus.Core.Storage
{
    public class NoteRepository : INoteRepository
    {
        private readonly DbContext _dbContext;
        public NoteRepository(DbContext dbContext)
        {
            _dbContext = dbContext;
        }
        public async Task DeleteAsync(string id)
        {
            using var conn = _dbContext.CreateConnection();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "DELETE FROM Notes WHERE ID = $id";
            cmd.Parameters.AddWithValue("id", id);
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task<IEnumerable<Note>> GetAllAsync()
        {
            var notes = new List<Note>();
            using var conn = _dbContext.CreateConnection();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT Id, Title, Content, CreatedAt, UpdatedAt FROM Notes";
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                notes.Add(new Note
                {
                    Id = reader.GetString(0),
                    Title = reader.GetString(1),
                    Content = reader.GetString(2),
                    CreatedAt = DateTime.Parse(reader.GetString(3)),
                    UpdatedAt = DateTime.Parse(reader.GetString(4))
                });
            }
            return notes;
        }

        public async Task<Note?> GetByIdAsync(string id)
        {
            using var conn = _dbContext.CreateConnection();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT Id, Title, Content, CreatedAt, UpdatedAt FROM Notes WHERE Id = $id";
            cmd.Parameters.AddWithValue("$id", id);

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                return new Note
                {
                    Id = reader.GetString(0),
                    Title = reader.GetString(1),
                    Content = reader.GetString(2),
                    CreatedAt = DateTime.Parse(reader.GetString(3)),
                    UpdatedAt = DateTime.Parse(reader.GetString(4))
                };
            }
            return null;

        }

        public async Task InsertAsync(Note note)
        {
            using var conn = _dbContext.CreateConnection();
            var cmd = conn.CreateCommand();
            cmd.CommandText = @" 
                INSERT INTO Notes (Id, Title, Content, CreatedAt, UpdatedAt)
                VALUES ($id, $title, $content, $created, $updated)";
            cmd.Parameters.AddWithValue("$id", note.Id);
            cmd.Parameters.AddWithValue("$title", note.Title);
            cmd.Parameters.AddWithValue("$content", note.Content);
            cmd.Parameters.AddWithValue("$created", note.CreatedAt);
            cmd.Parameters.AddWithValue("$updated", note.UpdatedAt);

            await cmd.ExecuteNonQueryAsync();
        }

        public async Task UpdateAsync(Note note)
        {
            using var conn = _dbContext.CreateConnection();
            var cmd = conn.CreateCommand();
            cmd.CommandText = @" 
                UPDATE Notes
                SET Title = $title, Content = $content, UpdatedAt = $updated
                WHERE Id = $id";
            cmd.Parameters.AddWithValue("$id", note.Id);
            cmd.Parameters.AddWithValue("$title", note.Title);
            cmd.Parameters.AddWithValue("$content", note.Content);
            cmd.Parameters.AddWithValue("$updated", note.UpdatedAt);

            await cmd.ExecuteNonQueryAsync();
        }
    }
}
