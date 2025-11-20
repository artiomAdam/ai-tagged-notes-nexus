using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Interactivity;
using Avalonia.Media;
using AvRichTextBox;
using Nexus.Desktop.ViewModels;
using System;
using System.Linq;

namespace Nexus.Desktop.Views;

public partial class NoteEditorView : UserControl
{
    private RichTextBox _editor;
    public NoteEditorView()
    {
        InitializeComponent();
        _editor = Editor;
        _editor.FlowDocument.Selection_Changed += Editor_SelectionChanged;
        // hook to DataContext set by parent (MainWindow)
        this.AttachedToVisualTree += (_, _) =>
        {
            if (DataContext is NoteEditorViewModel vm && this.FindControl<RichTextBox>("Editor") is { } editor)
                vm.AttachEditor(editor);
        };

        LoadFonts();
    }

    private void LoadFonts()
    {
        var fonts = FontManager.Current.SystemFonts
            .Select(f => f.Name)
            .ToList();

        FontFamilyCombo.ItemsSource = fonts;

        if (fonts.Contains("Arial"))
            FontFamilyCombo.SelectedItem = "Arial";
    }

    private void FontColorPicker_ColorChanged(object? sender, ColorChangedEventArgs e)
    {
        var brush = new SolidColorBrush(e.NewColor);
        _editor!.FlowDocument.Selection.ApplyFormatting(ForegroundProperty, brush);
    }

    private void FontBgPicker_ColorChanged(object? sender, ColorChangedEventArgs e)
    {
        var brush = new SolidColorBrush(e.NewColor);
        _editor!.FlowDocument.Selection.ApplyFormatting(Inline.BackgroundProperty, brush);
    }

    private void FontSizeCombo_Changed(object? sender, SelectionChangedEventArgs e)
    {
        if (e.AddedItems.Count == 0) return;
        if (e.AddedItems[0] is ComboBoxItem item && double.TryParse(item.Content!.ToString(), out double size))
        {
            _editor!.FlowDocument.Selection.ApplyFormatting(Inline.FontSizeProperty, size);
        }
    }

    private void FontFamilyCombo_Changed(object? sender, SelectionChangedEventArgs e)
    {
        if (e.AddedItems.Count == 0)
            return;

        if (e.AddedItems[0] is string fontName)
        {
            var font = FontFamily.Parse(fontName);
            _editor!.FlowDocument.Selection.ApplyFormatting(Inline.FontFamilyProperty, font);
        }
    }

    private void BoldButton_Click(object? sender, RoutedEventArgs e)
    {
        _editor!.FlowDocument.Selection.ApplyFormatting(
            Inline.FontWeightProperty,
            FontWeight.Bold);
    }

    private void ItalicButton_Click(object? sender, RoutedEventArgs e)
    {
        _editor!.FlowDocument.Selection.ApplyFormatting(
            Inline.FontStyleProperty,
            FontStyle.Italic);
    }


    private void UnderlineButton_Click(object? sender, RoutedEventArgs e)
    {
        var underline = new TextDecoration
        {
            Location = TextDecorationLocation.Underline
        };

        var collection = new TextDecorationCollection { underline };

        _editor!.FlowDocument.Selection.ApplyFormatting(
            Inline.TextDecorationsProperty,
            collection);
    }
    private void Editor_SelectionChanged(TextRange selection)
    {
        UpdateFontFamilyUI();
        UpdateFontSizeUI();
        UpdateForegroundUI();
        UpdateBackgroundUI();
    }
    private void UpdateFontFamilyUI()
    {
        var value = _editor.FlowDocument.Selection.GetFormatting(Inline.FontFamilyProperty);

        if (value is FontFamily ff)
            FontFamilyCombo.SelectedItem = ff.Name;   // assuming ComboBoxItem strings
        else
            FontFamilyCombo.SelectedIndex = -1;       // mixed or none
    }
    private void UpdateFontSizeUI()
    {
        var value = _editor.FlowDocument.Selection.GetFormatting(Inline.FontSizeProperty);

        if (value is double size)
        {
            foreach (ComboBoxItem item in FontSizeCombo.Items)
            {
                if (double.TryParse(item.Content!.ToString(), out double num) && num == size)
                {
                    FontSizeCombo.SelectedItem = item;
                    return;
                }
            }

            FontSizeCombo.SelectedIndex = -1;
        }
        else
        {
            FontSizeCombo.SelectedIndex = -1;
        }
    }

    private void UpdateForegroundUI()
    {
        var value = _editor.FlowDocument.Selection.GetFormatting(ForegroundProperty);

        if (value is SolidColorBrush brush)
            FontColorPicker.Color = brush.Color;
        else
            ClearColorPicker(FontColorPicker);
    }

    private void UpdateBackgroundUI()
    {
        var value = _editor.FlowDocument.Selection.GetFormatting(Inline.BackgroundProperty);

        if (value is SolidColorBrush brush)
            HighlightColorPicker.Color = brush.Color;
        else
            ClearColorPicker(HighlightColorPicker);
    }


    private void ClearColorPicker(ColorPicker cp)
    {
        cp.Tag = "mixed";
        cp.Color = Colors.Transparent;
    }
}
