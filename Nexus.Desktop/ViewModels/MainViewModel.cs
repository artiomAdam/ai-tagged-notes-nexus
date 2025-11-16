using Nexus.Core.Interfaces;
using Nexus.Core.Models;
using Nexus.Core.Services;
using Nexus.Core.Utilities;
using ReactiveUI;
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

        private readonly NoteEditorViewModel _noteEditor;

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
                if(_selectedNote != null && _selectedNote != value)
                {
                    SaveNote();
                }
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

        private Topic? _selectedTopic;
        public Topic? SelectedTopic
        {
            get => _selectedTopic;
            set => SetProperty(ref _selectedTopic, value);
        }


        private readonly INoteSaveService _saveService;

        public MainViewModel(INoteRepository noteRepo, ITopicsRepository topicsRepo, INoteTopicsRepository noteTopicsRepo, NoteEditorViewModel noteEditor, INoteSaveService saveService)
        {
            _noteRepo = noteRepo;
            _topicsRepo = topicsRepo;
            _noteTopicsRepo = noteTopicsRepo;
            _noteEditor = noteEditor;
            _noteEditor.TopicsChanged += async () => await LoadTopicsAsync();
            _saveService = saveService;


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
            await LoadNotesAsync();
            await LoadTopicsAsync();
        }

        private async Task LoadNotesAsync()
        {
            var notes = (await _noteRepo.GetAllAsync()).ToList();
            NotesHierarchy.Clear();
            var hierarchy = BuildNoteHierarchy(notes);
            foreach (var n in hierarchy)
                NotesHierarchy.Add(n);
        }

        private async Task LoadTopicsAsync()
        {
            var prevSelectedNote = SelectedNote;
            var topics = (await _topicsRepo.GetAllAsync()).ToList();
            var noteMap = (await _noteRepo.GetAllAsync()).ToDictionary(n => n.Id);

            // switch to UI thread for collection updates
            await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
            {
                TopicsHierarchy.Clear();
            });

            foreach (var topic in topics)
            {
                topic.Notes.Clear();
                var noteIds = await _noteTopicsRepo.GetNotesForTopicAsync(topic.Id);
                foreach (var id in noteIds)
                    if (noteMap.TryGetValue(id, out var note))
                        topic.Notes.Add(note);

                await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                {
                    TopicsHierarchy.Add(topic);
                });
            }
            if (prevSelectedNote != null)
            {
                var restored = noteMap.TryGetValue(prevSelectedNote.Id, out var found) ? found : null;
                if (restored != null)
                {
                    SelectedNote = restored;
                    NoteEditor.LoadNote(restored);
                }
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
            if (SelectedNote == null) return;

            var content = NoteEditor.GetEditedNote().Content;
            await _saveService.SaveAsync(SelectedNote, content);

            NoteEditor.RaisePropertyChanged(nameof(NoteEditor.FooterText));
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
            await LoadTopicsAsync();
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
