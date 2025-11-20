using Nexus.Core.Models;

namespace Nexus.Core.Services
{
    public class NoteEvents
    {
        public Func<Note, string>? RequestNoteContent;
        public event Action<Note>? NoteUpdated;
        public event Action<Note>? TopicsChanged;
        public event Action<string>? SearchRequested;
        public event Action<Note, string>? ContentChanged;
        public event Action<Note, string>? SaveRequested;

        public void RaiseNoteUpdated(Note note) => NoteUpdated?.Invoke(note);
        public void RaiseTopicsChanged(Note note) => TopicsChanged?.Invoke(note);
        public void RaiseSearchRequested(string query) => SearchRequested?.Invoke(query);
        public void RaiseContentChanged(Note note, string content) => ContentChanged?.Invoke(note, content);
        public void RaiseSaveRequested(Note note, string content) => SaveRequested?.Invoke(note, content);
    }
}
