using Nexus.Core.Models;
using ReactiveUI;
using AvRichTextBox;
using System;


namespace Nexus.Desktop.ViewModels
{
    public class NoteEditorViewModel : ReactiveObject
    {
        private Note _currentNote = new();
        private RichTextBox? _editor;

        public Note CurrentNote
        {
            get => _currentNote;
            set => this.RaiseAndSetIfChanged (ref _currentNote, value);
        }
        

        public NoteEditorViewModel()
        {
        }

        public void LoadNote(Note note)
        {
            CurrentNote = note;
            if (_editor == null) return;
            if (string.IsNullOrEmpty(note.Content))
                _editor.CloseDocument();
            else
                _editor.LoadXamlString(note.Content);
        }

        public Note GetEditedNote()
        {
            if (_editor != null)
                CurrentNote.Content = _editor.SaveXamlString();
            else
                CurrentNote.Content = string.Empty;

            CurrentNote.UpdatedAt = DateTime.UtcNow;
            return CurrentNote;
        }

        public void AttachEditor(RichTextBox editor)
        {
            _editor = editor;
            if (CurrentNote == null)
            {
                _editor.CloseDocument();
                return;
            }

            if (string.IsNullOrEmpty(CurrentNote.Content))
                _editor.CloseDocument();
            else
                _editor.LoadXamlString(CurrentNote.Content);
        }

    }
}
