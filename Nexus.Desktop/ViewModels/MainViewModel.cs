using Nexus.Core.Interfaces;
using Nexus.Core.Models;
using Nexus.Core.Utilities;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Nexus.Desktop.ViewModels
{
    public class MainViewModel : ObservableObject
    {
        private readonly INoteRepository _noteRepo;

        private NoteEditorViewModel _noteEditor = new();

        private bool _isHierarchyMode = true;
        public bool IsHierarchyMode
        {
            get => _isHierarchyMode;
            set
            {
                if (SetProperty(ref _isHierarchyMode, value))
                    ((RelayCommand)AddChildNoteCommand).RaiseCanExecuteChanged();
            }
        }
        public NoteEditorViewModel NoteEditor
        {
            get => _noteEditor;
            set => SetProperty(ref _noteEditor, value);
        }
        private Note? _selectedNote;
        public ObservableCollection<Note> NotesHierarchy { get; private set; } = new();
        public ObservableCollection<Topic> TopicsHierarchy { get; private set; } = new();

        private readonly INoteTopicsRepository _noteTopicsRepo;
        private readonly ITopicsRepository _topicsRepo;

        public Note? SelectedNote
        {
            get => _selectedNote;
            set
            {
                if (SetProperty(ref _selectedNote, value))
                {
                    NoteEditor.LoadNote(value);
                    ((RelayCommand)AddChildNoteCommand).RaiseCanExecuteChanged();
                }
            }
        }

        // Commands
        public ICommand DeleteNoteCommand { get; }
        public ICommand SaveNoteCommand { get; }
        public ICommand AddParentNoteCommand { get; }
        public ICommand AddChildNoteCommand { get; }

        private bool _isTopicsMode => !_isHierarchyMode;
        private Topic? _selectedTopic;
        public Topic? SelectedTopic
        {
            get => _selectedTopic;
            set => SetProperty(ref _selectedTopic, value);
        }


        public MainViewModel(INoteRepository noteRepo, ITopicsRepository topicsRepo, INoteTopicsRepository noteTopicsRepo)
        {
            _noteRepo = noteRepo;
            _topicsRepo = topicsRepo;
            _noteTopicsRepo = noteTopicsRepo;


            DeleteNoteCommand = new RelayCommand(async _ => await DeleteNoteAsync());
            SaveNoteCommand = new RelayCommand(_ => SaveNote());
            AddParentNoteCommand = new RelayCommand(async _ => await AddParentNoteAsync());
            AddChildNoteCommand = new RelayCommand(async _ => await AddChildNoteAsync(), _ => CanAddChildNote());
            
        }

        public async Task InitializeAsync()
        {
            await LoadNotesAndTopics();
        }

        private async Task LoadNotesAndTopics()
        {
            // TODO: does this need to be two seperate functions? should assess how slow it is in a further stress test
            var notes = (await _noteRepo.GetAllAsync()).ToList();
            var topics = (await _topicsRepo.GetAllAsync()).ToList();

            NotesHierarchy.Clear();
            var hierarchy = BuildNoteHierarchy(notes);
            foreach (var n in hierarchy)
                NotesHierarchy.Add(n);

            var noteMap = notes.ToDictionary(n => n.Id);
            TopicsHierarchy.Clear();
            foreach (var topic in topics)
            {
                topic.Notes.Clear();
                var noteIds = await _noteTopicsRepo.GetNotesForTopicAsync(topic.Id);
                foreach (var id in noteIds)
                {
                    if (noteMap.TryGetValue(id, out var note))
                        topic.Notes.Add(note);
                }
                TopicsHierarchy.Add(topic);
            }
        }

        private ObservableCollection<Note> BuildNoteHierarchy(IEnumerable<Note> flatNotes)
        {
            var lookup = flatNotes.ToDictionary(n => n.Id);
            foreach (var note in flatNotes)
            {
                if (!string.IsNullOrEmpty(note.ParentId) && lookup.TryGetValue(note.ParentId, out var parent))
                    parent.Children.Add(note);
            }
            return new ObservableCollection<Note>(flatNotes.Where(n => string.IsNullOrEmpty(n.ParentId)));
        }


        private async void SaveNote()
        {
            if(SelectedNote != null)
            {
                var editedNote = NoteEditor.GetEditedNote();
                await _noteRepo.UpdateAsync(editedNote);
            }
        }

        private async Task DeleteNoteAsync()
        {
            if (SelectedNote is null) return;

            if (_isHierarchyMode && SelectedNote.Children.Any())
            {
                Console.WriteLine("Cannot delete note with children.");
                return;
            }

            await _noteTopicsRepo.DeleteByNoteIdAsync(SelectedNote.Id);
            await _noteRepo.DeleteAsync(SelectedNote.Id);

            if (_isHierarchyMode)
            {
                if (!string.IsNullOrEmpty(SelectedNote.ParentId))
                    FindParentNote(NotesHierarchy, SelectedNote.ParentId)?.Children.Remove(SelectedNote);
                else
                    NotesHierarchy.Remove(SelectedNote);
            }
            else
            {
                SelectedTopic?.Notes.Remove(SelectedNote);
            }

            SelectedNote = null;
            NoteEditor.LoadNote(new Note());
        }

        private async Task AddParentNoteAsync()
        {
            var newNote = new Note
            {
                Title = "Untitled",
                Content = "",
                ParentId = null
            };

            await _noteRepo.InsertAsync(newNote);

            if (_isHierarchyMode)
            {
                NotesHierarchy.Add(newNote);
            }
            else if (SelectedTopic is not null)
            {
                await _noteTopicsRepo.AddTopicToNoteAsync(newNote.Id, SelectedTopic.Id);
                SelectedTopic.Notes.Add(newNote);
            }

            SelectedNote = newNote;
            NoteEditor.LoadNote(newNote);
        }

        private bool CanAddChildNote()
        {
            return _isHierarchyMode && SelectedNote != null;
        }

        private async Task AddChildNoteAsync()
        {
            if (!_isHierarchyMode || SelectedNote is null)
                return;

            var child = new Note
            {
                Title = $"{SelectedNote.Title} child",
                Content = "",
                ParentId = SelectedNote.Id
            };

            await _noteRepo.InsertAsync(child);
            SelectedNote.Children.Add(child);

            SelectedNote = child;
            NoteEditor.LoadNote(child);
        }

        private Note? FindParentNote(IEnumerable<Note> notes, string parentId)
        {
            foreach (var n in notes)
            {
                if (n.Id == parentId)
                    return n;

                var found = FindParentNote(n.Children, parentId);
                if (found != null)
                    return found;
            }
            return null;
        }
    }
}
