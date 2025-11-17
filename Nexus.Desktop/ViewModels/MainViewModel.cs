using Nexus.Core.Interfaces;
using Nexus.Core.Models;
using Nexus.Core.Services;
using Nexus.Core.Utilities;
using Nexus.Desktop.ViewModels.Enums;
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
        public ObservableCollection<Note> SimilarNotes { get; private set; } = new();
        private readonly INoteRepository _noteRepo;


        private readonly NoteEditorViewModel _noteEditor;
        public NoteEditorViewModel NoteEditor
        {
            get => _noteEditor;
        }

        
       
        private Note? _selectedNote;
        public Note? SelectedNote
        {
            get => _selectedNote;
            set
            {
                if (_selectedNote != null && _selectedNote != value)
                {
                    SaveNote();
                }
                if (SetProperty(ref _selectedNote, value))
                {
                    NoteEditor.LoadNote(value);
                    UpdateSimilarNotes();
                    ((RelayCommand)AddChildNoteCommand).RaiseCanExecuteChanged();
                }
            }
        }
        public ObservableCollection<Note> NotesHierarchy { get; private set; } = new();
        public ObservableCollection<Topic> TopicsHierarchy { get; private set; } = new();

        private readonly INoteTopicsRepository _noteTopicsRepo;
        private readonly ITopicsRepository _topicsRepo;

        

        private object? _treeSelection;
        public object? TreeSelection
        {
            get => _treeSelection;
            set
            {
                _treeSelection = value;
                OnPropertyChanged();

                if (value is Note n)
                {
                    SelectedNote = n;
                    SelectedTopic = null;
                }
                else if (value is Topic t)
                {
                    SelectedTopic = t;
                    SelectedNote = null;
                }
            }
        }

        private Topic? _selectedTopic;
        public Topic? SelectedTopic
        {
            get => _selectedTopic;
            set => SetProperty(ref _selectedTopic, value);
        }


        private NotesFilterType _selectedNotesFilter = NotesFilterType.AtoZ;
        public NotesFilterType SelectedNotesFilter
        {
            get => _selectedNotesFilter;
            set
            {
                if (SetProperty(ref _selectedNotesFilter, value))
                    ApplyNotesFilter();
            }
        }

        private TopicsFilterType _selectedTopicsFilter = TopicsFilterType.AtoZ;
        public TopicsFilterType SelectedTopicsFilter
        {
            get => _selectedTopicsFilter;
            set
            {
                if (SetProperty(ref _selectedTopicsFilter, value))
                    ApplyTopicsFilter();
            }
        }
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

        private List<Note> _allNotesCache = new();

        private string _searchText = "";
        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                {
                    ((RelayCommand)SearchCommand).RaiseCanExecuteChanged();

                    if (string.IsNullOrWhiteSpace(value))
                        _ = RestoreFullHierarchyAsync();
                }
            }
        }

        // bound to ComboBox.SelectedIndex
        private int _searchModeIndex;
        public int SearchModeIndex
        {
            get => _searchModeIndex;
            set => SetProperty(ref _searchModeIndex, value);
        }

        // helper to map index to enum
        private SearchModeType CurrentSearchMode => (SearchModeType)_searchModeIndex;




        // Commands
        public ICommand DeleteNoteCommand { get; }
        public ICommand SaveNoteCommand { get; }
        public ICommand AddParentNoteCommand { get; }
        public ICommand AddChildNoteCommand { get; }
        public ICommand RemoveTopicCommand { get; }
        public ICommand SearchCommand { get; }
        public ICommand ClearSearchCommand { get; }


        private readonly INoteSaveService _saveService;

        public MainViewModel(INoteRepository noteRepo, ITopicsRepository topicsRepo, INoteTopicsRepository noteTopicsRepo, NoteEditorViewModel noteEditor, INoteSaveService saveService)
        {
            _noteRepo = noteRepo;
            _topicsRepo = topicsRepo;
            _noteTopicsRepo = noteTopicsRepo;
            _noteEditor = noteEditor;
            _noteEditor.TopicsChanged += async () => await LoadTopicsAsync();
            _saveService = saveService;


            DeleteNoteCommand = new RelayCommand(async param => await DeleteNoteAsync(param as Note));
            SaveNoteCommand = new RelayCommand(_ => SaveNote());
            AddParentNoteCommand = new RelayCommand(async _ => await AddParentNoteAsync());
            AddChildNoteCommand = new RelayCommand(async _ => await AddChildNoteAsync(), _ => CanAddChildNote());
            RemoveTopicCommand = new RelayCommand(async _ => await RemoveTopicAsync());

        }

        public async Task InitializeAsync()
        {
            
            
            await LoadNotesAndTopics();
        }
        private async Task RemoveTopicAsync()
        {
            await _noteTopicsRepo.DeleteByTopicIdAsync(SelectedTopic!.Id);
            await _topicsRepo.DeleteAsync(SelectedTopic!.Id);
            _noteEditor.Predictor.RemoveTopicAsync(SelectedTopic!.Id);
            await LoadTopicsAsync();
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

            ApplyNotesFilter();
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
            ApplyTopicsFilter();
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

        private async Task DeleteNoteAsync(Note? noteToDelete)
        {
            if (noteToDelete is null) return;

            if (_isHierarchyMode && noteToDelete.Children.Any())
            {
                // TODO: currently cannot delete note with children, should we allow this?
                return;
            }

            await _noteTopicsRepo.DeleteByNoteIdAsync(noteToDelete.Id);
            await _noteRepo.DeleteAsync(noteToDelete.Id);

            if (_isHierarchyMode)
            {
                if (!string.IsNullOrEmpty(noteToDelete.ParentId))
                    FindParentNote(NotesHierarchy, noteToDelete.ParentId)?.Children.Remove(noteToDelete);
                else
                    NotesHierarchy.Remove(noteToDelete);
            }
            else
            {
                SelectedTopic?.Notes.Remove(noteToDelete);
            }

            if (SelectedNote != null && noteToDelete.Id == SelectedNote.Id)
            {
                SelectedNote = null;
                NoteEditor.LoadNote(new Note());
            }
            await LoadTopicsAsync(); // TODO: do we need this here? maybe if we're in notes mode, we don't need to load this and only load on switch to topics mode
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

        private async Task SearchAsync()
        {
            var query = SearchText.Trim().ToLower();
            if (string.IsNullOrWhiteSpace(query))
            {
                await RestoreFullHierarchyAsync();
                return;
            }

            IEnumerable<Note> matches = Enumerable.Empty<Note>();

            switch (CurrentSearchMode)
            {
                case SearchModeType.ByTitle:
                    matches = _allNotesCache.Where(n =>
                        !string.IsNullOrEmpty(n.Title) &&
                        n.Title.ToLower().Contains(query));
                    break;

                case SearchModeType.CurrentNote:
                    if (SelectedNote != null &&
                        !string.IsNullOrEmpty(SelectedNote.Content) &&
                        SelectedNote.Content.ToLower().Contains(query))
                    {
                        matches = new[] { SelectedNote };
                    }
                    break;

                case SearchModeType.AllNotes:
                    matches = _allNotesCache.Where(n =>
                        !string.IsNullOrEmpty(n.Content) &&
                        n.Content.ToLower().Contains(query));
                    break;
            }

            // Replace hierarchy with search results
            NotesHierarchy.Clear();
            foreach (var n in matches)
                NotesHierarchy.Add(n);

            OnPropertyChanged(nameof(NotesHierarchy));
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

        private void ApplyNotesFilter()
        {
            

            var sorted = SelectedNotesFilter switch
            {
                NotesFilterType.AtoZ => NotesHierarchy.OrderBy(n => n.Title),
                NotesFilterType.ZtoA => NotesHierarchy.OrderByDescending(n => n.Title),
                NotesFilterType.Newest => NotesHierarchy.OrderByDescending(n => n.CreatedAt),
                NotesFilterType.Oldest => NotesHierarchy.OrderBy(n => n.CreatedAt),
                NotesFilterType.Topic => NotesHierarchy.OrderBy(n => n.Title), // placeholder
                _ => NotesHierarchy.AsEnumerable()
            };

            NotesHierarchy = new ObservableCollection<Note>(sorted);
            OnPropertyChanged(nameof(NotesHierarchy));
        }



        private void ApplyTopicsFilter()
        {
            

            var sorted = SelectedTopicsFilter switch
            {
                TopicsFilterType.AtoZ => TopicsHierarchy.OrderBy(t => t.Name),
                TopicsFilterType.ZtoA => TopicsHierarchy.OrderByDescending(t => t.Name),
                TopicsFilterType.Similarity => TopicsHierarchy.AsEnumerable(), // placeholder
                _ => TopicsHierarchy.AsEnumerable()
            };


            TopicsHierarchy = new ObservableCollection<Topic>(sorted);
            OnPropertyChanged(nameof(TopicsHierarchy));
        }

        private void UpdateSimilarNotes()
        {
            SimilarNotes.Clear();

            // No note selected? Just stop.
            if (SelectedNote == null)
                return;

            // TODO: Real similarity using embeddings
            // For now, leave empty so UI runs with no errors.
        }

        private async Task RestoreFullHierarchyAsync()
        {
            var notes = await LoadNotesAsync();
        }
    }
}
