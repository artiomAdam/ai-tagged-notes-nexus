using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Microsoft.Extensions.DependencyInjection;
using Nexus.Core.Models;
using Nexus.Desktop.ViewModels;
using Nexus.Desktop.ViewModels.Enums;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Nexus.Desktop.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        var mainVM = App.Services!.GetRequiredService<MainViewModel>();
        var editorVM = App.Services!.GetRequiredService<NoteEditorViewModel>();

        DataContext = mainVM;
        NoteEditorHost.DataContext = editorVM;

        this.AttachedToVisualTree += (_, _) =>
        {
            if (DataContext is MainViewModel vm)
            {
                vm.PropertyChanged += (_, e) =>
                {
                    if (e.PropertyName == nameof(MainViewModel.SelectedNote) && vm.SelectedNote != null)
                    {
                        Dispatcher.UIThread.Post(() =>
                        {
                            NotesTree.ScrollIntoView(vm.SelectedNote);
                        });
                    }
                };
            }
        };
    }




    protected override async void OnOpened(EventArgs e)
    {
        base.OnOpened(e);
        if (DataContext is MainViewModel vm)
            await vm.InitializeAsync();
    }

    private void FilterNotesChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (sender is ComboBox cb && cb.SelectedItem is ComboBoxItem item)
        {
            if (item.Tag is NotesFilterType filter)
            {
                if (DataContext is MainViewModel vm)
                    vm.SelectedNotesFilter = filter;
            }
        }
    }

    private void FilterTopicsChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (sender is ComboBox cb && cb.SelectedItem is ComboBoxItem item)
        {
            if (item.Tag is TopicsFilterType filter)
            {
                if (DataContext is MainViewModel vm)
                    vm.SelectedTopicsFilter = filter;
            }
        }
    }

}