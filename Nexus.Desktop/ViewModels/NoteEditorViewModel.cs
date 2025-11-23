using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Documents;
using Avalonia.Input;
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
using System.Threading;
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



        
        public ObservableCollection<Topic> LinkedTopics { get; } = new();
        
        


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
            }
        }

        // Storage:
        private readonly INoteTopicsRepository _noteTopicsRepo;
        private readonly ITopicsRepository _topicsRepo;

        // Services:
        private readonly INoteSaveService _saveService;

        // AutoSave:
        private CancellationTokenSource? _autosaveCts;
        private static readonly TimeSpan AutosaveDelay = TimeSpan.FromSeconds(2);



        // Commands:
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
            
            // Events:
            _events = events;
            _events.NoteUpdated += note => LoadNote(note);
            _events.TopicsChanged += note =>
            {
                if (note != null && _tagPredictor != null)
                {
                    _tagPredictor.RemoveTopicAsync(note.Id);
                }
            };
            _events.SearchRequested += query => HighlightSearchHit(query);
            _events.RequestNoteContent = note =>
            {
                if (CurrentNote == note && _editor != null)
                    return _editor.GetFullXamlString();
                return note.Content ?? "";
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
            
            
            PredictTopicCommand = new RelayCommand(async _ => await PredictTopicAsync());
            AddSuggestedTopicCommand = new RelayCommand(async param => await AddSuggestedTopic(param  as string));
            RemoveTopicCommand = new RelayCommand(async param => await RemoveTopicAsync(param as string));
            OpenCustomTopicDialogCommand = new RelayCommand(_ => OpenCustomTopicDialog());


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


        private void OnEditorTextChanged(object? sender, EventArgs e)
        {
            
            if (_editor == null || CurrentNote == null)
                return;

            if (!CurrentNote.HasUnsavedChanges)
                CurrentNote.HasUnsavedChanges = true;

            this.RaisePropertyChanged(nameof(FooterText));
            var plainText = ExtractPlainText(_editor.GetFullXamlString());
            if (string.IsNullOrWhiteSpace(plainText))
            {
                ClearPredictions();
                return;
            }

            // cancel previous pending autosave
            _autosaveCts?.Cancel();
            _autosaveCts = new CancellationTokenSource();
            var token = _autosaveCts.Token;

            _ = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(AutosaveDelay, token);
                    if (token.IsCancellationRequested)
                        return;

                    string content = await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        return _editor!.GetFullXamlString();
                    });

                    // keep the Note model in sync before saving
                    CurrentNote!.Content = content;
                    CurrentNote.UpdatedAt = DateTime.UtcNow;
                    CurrentNote.HasUnsavedChanges = false;

                    await _saveService.SaveAsync(CurrentNote, content);
                    await PredictTopicAsync();

                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        this.RaisePropertyChanged(nameof(FooterText));
                    });
                }
                catch (TaskCanceledException)
                {
                    // expected, user kept typing
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Autosave failed: {ex.Message}");
                }
            });

        }


        private async Task AddSuggestedTopic(string? topicName)
        {
            if (CurrentNote == null) return;

            if (string.IsNullOrWhiteSpace(topicName))
                return;

            PredictedTopics.Remove(topicName);
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
            if (note == null) return;
            CurrentNote = note;
            CurrentNote.HasUnsavedChanges = false;
            
            if (_editor == null) return;
            if (string.IsNullOrEmpty(note.Content))
                _editor.CloseDocument();
            else
                _editor.LoadXamlString(note.Content);

            this.RaisePropertyChanged(nameof(FooterText));

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
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    ClearPredictions();
                });
                return;
            }
            
            var namesOfLinkedTopics = LinkedTopics.Select(t => t.Name).ToList();
            var topTopics = await _tagPredictor.PredictTopTopics(CurrentNote, 3 + LinkedTopics.Count);
            if (topTopics.Count == 0)
            {
                return;
            }

            var lines = new List<string>();
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                ClearPredictions();
            });
            foreach (var (id, score) in topTopics)
            {
                if (PredictedTopics.Count == 3) break;
                var name = await _topicsRepo.GetNameByIdAsync(id);
                if (!namesOfLinkedTopics.Contains(name))
                {
                    lines.Add($"{name} ({score:F3})");
                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        if (!PredictedTopics.Contains(name))
                            PredictedTopics.Add(name);
                    });
                }
                
            }
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
            bool wasRemoved = false;
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

            if (PredictedTopics.Contains(chosen.Name))
            {
                PredictedTopics.Remove(chosen.Name);
                wasRemoved = true;
            }

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
            if(wasRemoved) await PredictTopicAsync();
        }

        private Window? GetMainWindow()
        {
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                return desktop.MainWindow;

            return null;
        }

        public void MarkAsEdited()
        {
            if (CurrentNote != null)
            {
                CurrentNote.HasUnsavedChanges = true;
                this.RaisePropertyChanged(nameof(FooterText));
            }
        }


    }
}
