using Avalonia.Controls;
using AvRichTextBox;
using Nexus.Desktop.ViewModels;

namespace Nexus.Desktop.Views;

public partial class NoteEditorView : UserControl
{
    public NoteEditorView()
    {
        InitializeComponent();

        // hook to DataContext set by parent (MainWindow)
        this.AttachedToVisualTree += (_, _) =>
        {
            if (DataContext is NoteEditorViewModel vm && this.FindControl<RichTextBox>("Editor") is { } editor)
                vm.AttachEditor(editor);
        };
    }
}
