using Avalonia.Media.Imaging;
using AvRichTextBox;
using Nexus.Core.Models;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;


namespace Nexus.Desktop.ViewModels
{
    public class NoteEditorViewModel : ReactiveObject
    {
        private Note _currentNote = new();
        private RichTextBox? _editor;
        private int _imgNum = 0;

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
                CurrentNote.Content = _editor.GetFullXamlString();
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

            _editor.ImagePasteRequested += async (sender, bitmap) =>
            {
                string folder = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                    $"KnowledgeNexus\\note_{_currentNote.Id}");
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
                Paragraph? currentPara = new();
                
                currentPara.Inlines.Add(container);
                _editor.FlowDocument.Blocks.Add(currentPara);

                CurrentNote.Content = _editor.GetFullXamlString();
                CurrentNote.UpdatedAt = DateTime.UtcNow;
            };
        }

    }
}
