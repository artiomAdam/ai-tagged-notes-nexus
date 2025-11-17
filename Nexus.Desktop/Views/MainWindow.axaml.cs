using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Nexus.Desktop.ViewModels;
using Nexus.Desktop.ViewModels.Enums;
using System;

namespace Nexus.Desktop.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        this.AttachedToVisualTree += (_, _) =>
        {
            if (DataContext is MainViewModel vm)
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