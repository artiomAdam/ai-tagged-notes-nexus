using Nexus.Core.Models;

namespace Nexus.Core.Services
{
    public class NoteEvents
    {
        public event Action<Note>? NoteUpdated;
        public event Action<Note>? TopicsChanged;

        public void RaiseNoteUpdated(Note note) => NoteUpdated?.Invoke(note);
        public void RaiseTopicsChanged(Note note) => TopicsChanged?.Invoke(note);
    }
}
