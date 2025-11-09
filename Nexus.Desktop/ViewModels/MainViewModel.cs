using Nexus.Core.Interfaces;
using Nexus.Core.Models;
using Nexus.Core.Utilities;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace Nexus.Desktop.ViewModels
{
    public class MainViewModel : ObservableObject
    {
        private readonly INoteRepository _noteRepo;

        private NoteEditorViewModel _noteEditor = new();
        public NoteEditorViewModel NoteEditor
        {
            get => _noteEditor;
            set => SetProperty(ref _noteEditor, value);
        }
        private Note? _selectedNote;
        public ObservableCollection<Note> Notes { get; } = new();

        public Note? SelectedNote
        {
            get => _selectedNote;
            set
            {
                if (SetProperty(ref _selectedNote, value) && value != null)
                    NoteEditor.LoadNote(value);
            }
        }

        // Commands
        public ICommand NewNoteCommand { get; }
        public ICommand DeleteNoteCommand { get; }
        public ICommand SaveNoteCommand { get; }

        public MainViewModel(INoteRepository repo)
        {
            _noteRepo = repo;

            NewNoteCommand = new RelayCommand(_ => CreateNewNote());
            DeleteNoteCommand = new RelayCommand(_ => DeleteNote());
            SaveNoteCommand = new RelayCommand(_ => SaveNote());

            LoadNotes();
        }

        private async void LoadNotes()
        {
            Notes.Clear();
            var notes = await _noteRepo.GetAllAsync();
            foreach (var n in notes)
            {
                Notes.Add(n);
            }
        }

        private async void CreateNewNote()
        {
            var note = new Note { Title = "Untitled", Content = "" };
            await _noteRepo.InsertAsync(note);
            Notes.Add(note);
            SelectedNote = note;
        }

        private async void SaveNote()
        {
            if(SelectedNote != null)
            {
                var editedNote = NoteEditor.GetEditedNote();
                await _noteRepo.UpdateAsync(editedNote);
            }
        }

        private async void DeleteNote()
        {
            if(SelectedNote != null)
            {
                await _noteRepo.DeleteAsync(SelectedNote.Id);
                Notes.Remove(SelectedNote);
                SelectedNote = null;
                NoteEditor.LoadNote(new Note());
            }
        }
    }
}
