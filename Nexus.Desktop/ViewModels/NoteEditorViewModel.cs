using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Documents;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using AvRichTextBox;
using Nexus.Core.Interfaces;
using Nexus.Core.Models;
using Nexus.Core.Services;
using Nexus.Core.Utilities;
using Nexus.Desktop.Views;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Numerics.Tensors;
using System.Reactive.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Input;


namespace Nexus.Desktop.ViewModels
{
    public class NoteEditorViewModel : ReactiveObject
    {

        // Topics:
            // predictions:
        private readonly TagPredictor _tagPredictor;
        public TagPredictor Predictor => _tagPredictor;
        public ObservableCollection<string> PredictedTopics { get; } = new();



        // topic picker (temporary?)
        private Topic? _selectedTopic;
        private TopicPickerItem? _selectedTopicPickerItem;
        private int _topicPickerSelectedIndex;
        public ObservableCollection<TopicPickerItem> TopicPickerItems { get; } = new();
        public TopicPickerItem? SelectedTopicPickerItem
        {
            get => _selectedTopicPickerItem;
            set
            {
                this.RaiseAndSetIfChanged(ref _selectedTopicPickerItem, value);
                OnTopicPickerChanged();
            }
        }
        public ObservableCollection<Topic> LinkedTopics { get; } = new();
        
        public Topic? SelectedTopic
        {
            get => _selectedTopic;
            set
            {
                this.RaiseAndSetIfChanged(ref _selectedTopic, value);
                if (value != null)
                {
                    // TODO: Maybe future funcitonality - select a topic and do something with it?
                }
            }
        }
        public int TopicPickerSelectedIndex
        {
            get => _topicPickerSelectedIndex;
            set => this.RaiseAndSetIfChanged(ref _topicPickerSelectedIndex, value);
        }



        // Extras Tab:
        private int _extrasTabIndex;
        public int ExtrasTabIndex
        {
            get => _extrasTabIndex;
            set => this.RaiseAndSetIfChanged(ref _extrasTabIndex, value);
        }

        // Footer:
        public string FooterText =>
                CurrentNote is null
                ? "No note selected"
                : $"{CurrentNote.Title} • Created: {CurrentNote.CreatedAt:g} • Last Updated: {CurrentNote.UpdatedAt:g}" +
                  (CurrentNote.HasUnsavedChanges ? " • ✖ Unsaved Changes" : "");
        private RichTextBox? _editor;
        private int _imgNum = 0; // TODO: check this out

        // Note:
        private Note? _currentNote;
        public Note? CurrentNote
        {
            get => _currentNote;
            set
            {
                this.RaiseAndSetIfChanged(ref _currentNote, value);
                _ = LoadLinkedTopicsAsync();
                _ = LoadTopicPickerAsync();
            }
        }

        // Storage:
        private readonly INoteTopicsRepository _noteTopicsRepo;
        private readonly ITopicsRepository _topicsRepo;

        // Services:
        private readonly INoteSaveService _saveService;



        // Commands:
        public ICommand ToggleBoldCommand { get; }
        public ICommand ToggleItalicCommand { get; }
        public ICommand ToggleUnderlineCommand { get; }
        public ICommand AddTopicCommand { get; }
        public ICommand PredictTopicCommand { get; }
        public ICommand AddSuggestedTopicCommand { get; }
        public ICommand RemoveTopicCommand { get; }
        public ICommand OpenCustomTopicDialogCommand { get; }

        private readonly NoteEvents _events;

        public NoteEditorViewModel(INoteTopicsRepository noteTopicsRepo, ITopicsRepository topicsRepo, 
                                    TagPredictor tagPredictor, INoteSaveService saveService, NoteEvents events)
        {

            _noteTopicsRepo = noteTopicsRepo;
            _topicsRepo = topicsRepo;
            _tagPredictor = tagPredictor;
            _saveService = saveService;
            _events = events;
            _events.NoteUpdated += note => LoadNote(note);
            _events.TopicsChanged += note =>
            {
                if (note != null && _tagPredictor != null)
                {
                    _tagPredictor.RemoveTopicAsync(note.Id);
                }
            };

            this.WhenAnyValue(
                            vm => vm.CurrentNote!.Title,
                            vm => vm.CurrentNote!.Content)
                            .Skip(1)
                            .Subscribe(_ =>
                            {
                                if (CurrentNote != null)
                                {
                                    CurrentNote.HasUnsavedChanges = true;
                                    this.RaisePropertyChanged(nameof(FooterText));
                                }
                            });

            _tagPredictor = new TagPredictor(_topicsRepo);
            _ = _tagPredictor.InitializeAsync();
            
            AddTopicCommand = new RelayCommand(async _ => await AddTopicAsync());
            PredictTopicCommand = new RelayCommand(async _ => await PredictTopicAsync());
            AddSuggestedTopicCommand = new RelayCommand(async param => await AddSuggestedTopic(param  as string));
            RemoveTopicCommand = new RelayCommand(async param => await RemoveTopicAsync(param as string));
            OpenCustomTopicDialogCommand = new RelayCommand(_ => OpenCustomTopicDialog());

            // those are like that because richtextbox needs to be updated only from the UI thread
            ToggleBoldCommand = new RelayCommand(_ =>
            {
                Dispatcher.UIThread.Post(() =>
                    _editor!.FlowDocument.Selection.ApplyFormatting(
                        Inline.FontWeightProperty,
                        FontWeight.Bold));
            });

            ToggleItalicCommand = new RelayCommand(_ =>
            {
                Dispatcher.UIThread.Post(() =>
                    _editor!.FlowDocument.Selection.ApplyFormatting(
                        Inline.FontStyleProperty,
                        FontStyle.Italic));
            });

            ToggleUnderlineCommand = new RelayCommand(_ =>
            {
                Dispatcher.UIThread.Post(() =>
                    _editor!.FlowDocument.Selection.ApplyFormatting(
                        Inline.TextDecorationsProperty,
                        TextDecorations.Underline));
            });

        }

        
        public async Task RemoveTopicAsync(string? topicId)
        {
            if (CurrentNote == null || topicId == null) return;

            await _noteTopicsRepo.RemoveTopicFromNoteAsync(CurrentNote.Id, topicId);
            _ = LoadLinkedTopicsAsync();
            _ = PredictTopicAsync();

            _events.RaiseTopicsChanged(CurrentNote!);



        }
        public void AttachEditor(RichTextBox editor)
        {
            if (_editor == editor)
                return;

            if (_editor != null)
                _editor.ImagePasteRequested -= OnImagePasteRequested;

            _editor = editor;


            _editor.ImagePasteRequested += OnImagePasteRequested;
            _editor.KeyDown += OnEditorTextChanged;
        }

        private int _textChangeCounter = 0;
        private void OnEditorTextChanged(object? sender, EventArgs e)
        {
            // after like 10 changes save
            if(string.IsNullOrWhiteSpace(ExtractPlainText(_editor!.GetFullXamlString())))
            {
                ClearPredictions();
                return;
            }
            // make prediction
            if(_textChangeCounter < 10)
            {
                _textChangeCounter++;
                return;
            }
            _textChangeCounter = 0;

            if (CurrentNote != null)
            {
                string content = _editor!.GetFullXamlString();
                _ = _saveService.SaveAsync(CurrentNote, content);
                
                _ = PredictTopicAsync();
            }

        }


        private async Task AddTopicAsync()
        {
            string? input = await Prompt.ShowAsync("Add Topic", "Enter new topic:");
            if (string.IsNullOrWhiteSpace(input)) return;
            if (CurrentNote is null) return;

            Topic newTopic = new() { Name = input };
            await _topicsRepo.InsertAsync(newTopic);

            string? topicId = await _topicsRepo.GetIdByNameAsync(input);

            if (topicId == null) return;

            await _tagPredictor.AddOrUpdateTopicAsync(topicId, input);
            await _noteTopicsRepo.AddTopicToNoteAsync(CurrentNote.Id, topicId);
            _ = LoadLinkedTopicsAsync();

            _events.RaiseTopicsChanged(CurrentNote!);


        }

        private async Task AddSuggestedTopic(string? topicName)
        {
            if (CurrentNote == null) return;

            if (string.IsNullOrWhiteSpace(topicName))
                return;
            string? currentTopicId = await _topicsRepo.GetIdByNameAsync(topicName);
            if(currentTopicId  == null) return;

            await _noteTopicsRepo.AddTopicToNoteAsync(CurrentNote.Id, currentTopicId);
            await LoadLinkedTopicsAsync();
            await PredictTopicAsync();
            _events.RaiseTopicsChanged(CurrentNote!);

        }
        private async Task LoadLinkedTopicsAsync()
        {
            LinkedTopics.Clear();
            if (CurrentNote is null) return;

            var topicIds = await _noteTopicsRepo.GetTopicsForNoteAsync(CurrentNote.Id);
            var allTopics = await _topicsRepo.GetAllAsync();

            foreach (var t in allTopics.Where(t => topicIds.Contains(t.Id)))
                LinkedTopics.Add(t);
        }

        public void LoadNote(Note? note)
        {
            CurrentNote = note;
            if (note == null) return;
            
            if (_editor == null) return;
            if (string.IsNullOrEmpty(note.Content))
                _editor.CloseDocument();
            else
                _editor.LoadXamlString(note.Content);

            _ = PredictTopicAsync();

        }




        public Note GetEditedNote()
        {
            if (CurrentNote == null) return new Note();
            if (_editor != null)
                CurrentNote.Content = _editor.GetFullXamlString();
            else
                CurrentNote.Content = string.Empty;

            CurrentNote.UpdatedAt = DateTime.UtcNow;
            return CurrentNote;
        }



        private void OnImagePasteRequested(object? sender, Bitmap bitmap)
        {
            if (CurrentNote == null) return;

            string folder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                $"KnowledgeNexus\\note_{CurrentNote.Id}");
            Directory.CreateDirectory(folder);

            string filename = $"img_{_imgNum++}.png";
            string fullPath = Path.Combine(folder, filename);

            using (var fs = File.Create(fullPath))
                bitmap.Save(fs);

            var img = new Avalonia.Controls.Image
            {
                Source = new Bitmap(fullPath),
                Width = 80,
                Height = 80,
                Tag = fullPath,
            };

            var container = new EditableInlineUIContainer(img, fullPath);
            var currentPara = new Paragraph();
            currentPara.Inlines.Add(container);
            _editor!.FlowDocument.Blocks.Add(currentPara);

            CurrentNote.Content = _editor.GetFullXamlString();
            CurrentNote.UpdatedAt = DateTime.UtcNow;
        }
        private void ClearPredictions()
        {
            PredictedTopics.Clear();
        }

        
        private async Task PredictTopicAsync()
        {
            if (CurrentNote == null) return;
            if (string.IsNullOrEmpty(ExtractPlainText(CurrentNote.Content)))
            {
                ClearPredictions();
                return;
            }
            
            var namesOfLinkedTopics = LinkedTopics.Select(t => t.Name).ToList();
            var topTopics = await _tagPredictor.PredictTopTopics(CurrentNote, 3 + LinkedTopics.Count);
            if (topTopics.Count == 0)
            {
                return;
            }

            var lines = new List<string>();
            ClearPredictions();
            foreach (var (id, score) in topTopics)
            {
                if (PredictedTopics.Count == 3) break;
                var name = await _topicsRepo.GetNameByIdAsync(id);
                if (!namesOfLinkedTopics.Contains(name))
                {
                    lines.Add($"{name} ({score:F3})");
                    PredictedTopics.Add(name);
                }
                
            }
        }
        private async void OnTopicPickerChanged()
        {
            if (SelectedTopicPickerItem == null)
                return;

            if (SelectedTopicPickerItem.IsNew)
            {
                AddTopicCommand.Execute(null);
                await LoadTopicPickerAsync(); // refresh
                return;
            }

            // Existing topic selected → just add it to the note
            if (CurrentNote != null && SelectedTopicPickerItem.TopicId != null)
            {
                await _noteTopicsRepo.AddTopicToNoteAsync(CurrentNote.Id, SelectedTopicPickerItem.TopicId);
                await LoadLinkedTopicsAsync();
            }
            SelectedTopicPickerItem = null;
            TopicPickerSelectedIndex = -1;
        }

        public void HighlightSearchHit(string query)
        {
            if (_editor == null || string.IsNullOrWhiteSpace(query))
                return;

            Dispatcher.UIThread.Post(() =>
            {
                var doc = _editor.FlowDocument;
                if (doc == null)
                    return;

                string text = doc.Text;  // AvRichTextBox gives full text here

                var matches = System.Text.RegularExpressions.Regex.Matches(text, Regex.Escape(query), RegexOptions.IgnoreCase);

                // Choose match that is AFTER current selection
                var currentEnd = doc.Selection.End;
                var match = matches.Cast<Match>().FirstOrDefault(m => m.Index >= currentEnd);

                // If none found → wrap to beginning
                match ??= matches.Cast<Match>().FirstOrDefault();

                if (match != null)
                {
                    doc.Select(match.Index, query.Length);
                    _editor.ScrollToSelection();
                }
                // If no matches: do nothing (you can add UI feedback later)
            });
        }
        private async Task LoadTopicPickerAsync()
        {
            TopicPickerItems.Clear();

            // First "New Topic..." entry
            TopicPickerItems.Add(new TopicPickerItem
            {
                DisplayName = "➕ New Topic…",
                IsNew = true
            });

            var topics = await _topicsRepo.GetAllAsync();

            foreach (var t in topics)
            {
                TopicPickerItems.Add(new TopicPickerItem
                {
                    DisplayName = t.Name,
                    TopicId = t.Id,
                    IsNew = false
                });
            }
        }

        private static string ExtractPlainText(string xaml)
        {
            if (string.IsNullOrWhiteSpace(xaml))
                return string.Empty;
            string noTags = System.Text.RegularExpressions.Regex.Replace(
                xaml,
                "<[^>]+>",
                string.Empty);
            return System.Net.WebUtility.HtmlDecode(noTags).Trim();
        }

        private async void OpenCustomTopicDialog()
        {
            if (CurrentNote == null) return;
            var allTopics = await _topicsRepo.GetAllAsync();

            var available = allTopics
                .Where(t => !LinkedTopics.Any(lt => lt.Id == t.Id))
                .ToList();

            var dialog = new TopicPickerDialog(available);

            // Show dialog and get Topic? as return value
            var owner = GetMainWindow();
            if (owner == null) return;

            var chosen = await dialog.ShowDialog<Topic?>(owner);

            if (chosen == null)
                return; // user cancelled

            // If new topic (no ID yet)
            var existingTopics = await _topicsRepo.GetAllAsync();
            bool existsInDb = existingTopics.Any(t => t.Id == chosen.Id);
            if (!existsInDb)
            {
                
                await _topicsRepo.InsertAsync(chosen);
                chosen.Id = await _topicsRepo.GetIdByNameAsync(chosen.Name);
                await _tagPredictor.AddOrUpdateTopicAsync(chosen.Id, chosen.Name);
            }

            // Link to note
            await _noteTopicsRepo.AddTopicToNoteAsync(CurrentNote.Id, chosen.Id);
            LinkedTopics.Add(chosen);
        }

        private Window? GetMainWindow()
        {
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                return desktop.MainWindow;

            return null;
        }


    }
}
