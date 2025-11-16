using Avalonia.Controls.Documents;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using AvRichTextBox;
using Nexus.Core.Interfaces;
using Nexus.Core.Models;
using Nexus.Core.Services;
using Nexus.Core.Utilities;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;


namespace Nexus.Desktop.ViewModels
{
    public class NoteEditorViewModel : ReactiveObject
    {
        private readonly TagPredictor _tagPredictor;
        private int _extrasTabIndex;
        public int ExtrasTabIndex
        {
            get => _extrasTabIndex;
            set => this.RaiseAndSetIfChanged(ref _extrasTabIndex, value);
        }

        public string FooterText =>
                CurrentNote is null
                ? "No note selected"
                : $"{CurrentNote.Title} • Created: {CurrentNote.CreatedAt:g} • Last Updated: {CurrentNote.UpdatedAt:g}" +
                  (CurrentNote.HasUnsavedChanges ? " • ✖ Unsaved Changes" : "");
        private RichTextBox? _editor;
        private int _imgNum = 0;

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

        private readonly INoteTopicsRepository _noteTopicsRepo;
        private readonly ITopicsRepository _topicsRepo;
        public ObservableCollection<Topic> LinkedTopics { get; } = new();
        private Topic? _selectedTopic;
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

        public event Func<Task>? TopicsChanged;

        // Commands:
        public ICommand ToggleBoldCommand { get; }
        public ICommand ToggleItalicCommand { get; }
        public ICommand ToggleUnderlineCommand { get; }
        public ICommand AddTopicCommand { get; }
        public ICommand PredictTopicCommand { get; }

        private string? _predictedTopicName;
        public string? PredictedTopicName
        {
            get => _predictedTopicName;
            set
            {
                this.RaiseAndSetIfChanged(ref _predictedTopicName, value);
                if(value != null)
                {
                    //TODO:
                }
            }
        }

        private readonly INoteSaveService _saveService;
        public NoteEditorViewModel(INoteTopicsRepository noteTopicsRepo, ITopicsRepository topicsRepo, TagPredictor tagPredictor, INoteSaveService saveService)
        {

            _noteTopicsRepo = noteTopicsRepo;
            _topicsRepo = topicsRepo;
            _tagPredictor = tagPredictor;
            _saveService = saveService;

            this.WhenAnyValue(
                            vm => vm.CurrentNote!.Title,
                            vm => vm.CurrentNote!.Content)
                            .Skip(1) // ignore initial load
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
            // those are like that because trying to apply formatting through the UI thread crashes the app...
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
            await LoadLinkedTopicsAsync();

            if (TopicsChanged != null)
                await TopicsChanged.Invoke();


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

        public void LoadNote(Note note)
        {
            CurrentNote = note;
            if (note == null) return;
            
            if (_editor == null) return;
            if (string.IsNullOrEmpty(note.Content))
                _editor.CloseDocument();
            else
                _editor.LoadXamlString(note.Content);
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

        private async Task PredictTopicAsync()
        {
            if (CurrentNote == null) return;

            var topTopics = _tagPredictor.PredictTopTopics(CurrentNote, 3);
            if (topTopics.Count == 0)
            {
                PredictedTopicName = "No topics available.";
                return;
            }

            var lines = new List<string>();
            foreach (var (id, score) in topTopics)
            {
                var name = await _topicsRepo.GetNameByIdAsync(id);
                lines.Add($"{name ?? "(Unknown)"} ({score:F3})");
            }
            
            PredictedTopicName = string.Join("\n", lines);
        }

    }
}
