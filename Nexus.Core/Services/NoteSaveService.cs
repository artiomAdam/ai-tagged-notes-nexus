using Nexus.Core.Interfaces;
using Nexus.Core.Models;
namespace Nexus.Core.Services
{
    public class NoteSaveService : INoteSaveService
    {
        private readonly INoteRepository _repo;

        public NoteSaveService(INoteRepository repo)
        {
            _repo = repo;
        }

        public async Task SaveAsync(Note note, string content)
        {
            note.Content = content;
            note.UpdatedAt = DateTime.UtcNow;
            note.HasUnsavedChanges = false;

            await _repo.UpdateAsync(note);
        }
    }

}
