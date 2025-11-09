using Avalonia.Controls;
using Avalonia.Media;
using AvRichTextBox;
using Microsoft.Extensions.DependencyInjection;
using Nexus.Desktop.ViewModels;
using System;
using System.Collections.Generic;

namespace Nexus.Desktop.Views;

public partial class NoteEditorView : UserControl
{
    
    public NoteEditorView()
    {
        InitializeComponent();
        Loaded += NoteEditorView_Loaded;
    }

    private void NoteEditorView_Loaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {

        DataContext ??= App.Services.GetRequiredService<NoteEditorViewModel>();

        var vm = (NoteEditorViewModel)DataContext!;
        vm.AttachEditor(NoteRTB);

        // Populate fonts
        var fonts = new List<string>();
        foreach (var font in FontManager.Current.SystemFonts)
            fonts.Add(font.Name);
        FontComboBox.ItemsSource = fonts;
        FontComboBox.SelectedItem = "Segoe UI";

        // Hook up handlers
        BoldButton.Click += BoldButton_Clicked;
        ItalicButton.Click += ItalicButton_Clicked;
        UnderlineButton.Click += UnderscoreButton_Clicked;

        FontComboBox.SelectionChanged += (_, _) => {
            if (FontComboBox.SelectedItem is string font)
                NoteRTB.FlowDocument.Selection.ApplyFormatting(FontFamilyProperty, new FontFamily(font));
        };

        FontSizeUpDown.ValueChanged += (_, args) => {
            if (args.NewValue.HasValue)
                NoteRTB.FlowDocument.Selection.ApplyFormatting(FontSizeProperty, (double)args.NewValue.Value);
        };

        FontColorPicker.ColorChanged += FontColor_ColorChanged;

        HighlightColorPicker.ColorChanged += HighlightColor_ColorChanged;



    }

    private void BoldButton_Clicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        NoteRTB.FlowDocument.Selection.ApplyFormatting(FontWeightProperty, FontWeight.Bold);
    }

    private void ItalicButton_Clicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        NoteRTB.FlowDocument.Selection.ApplyFormatting(FontStyleProperty, FontStyle.Italic);
    }

    private void UnderscoreButton_Clicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        //NoteRTB.FlowDocument.Selection.ApplyFormatting(TextDecorationsProperty, TextDecorations.Underline);
    }

    private void FontColor_ColorChanged(object? sender, ColorChangedEventArgs e)
    {
        SolidColorBrush hBrush = new(e.NewColor);
        NoteRTB.FlowDocument.Selection.ApplyFormatting(ForegroundProperty, hBrush);
    }

    private void HighlightColor_ColorChanged(object? sender, ColorChangedEventArgs e)
    {
        SolidColorBrush hBrush = new(e.NewColor);
        NoteRTB.FlowDocument.Selection.ApplyFormatting(BackgroundProperty, hBrush);
    }
}
