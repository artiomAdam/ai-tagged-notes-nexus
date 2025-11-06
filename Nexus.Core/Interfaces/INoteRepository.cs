using Nexus.Core.Models;

namespace Nexus.Core.Interfaces
{
    public interface INoteRepository
    {
        Task<IEnumerable<Note>> GetAllAsync();
        Task<Note?> GetByIdAsync(string id);
        Task InsertAsync(Note note);
        Task UpdateAsync(Note note);
        Task DeleteAsync(string id);
    }
}
