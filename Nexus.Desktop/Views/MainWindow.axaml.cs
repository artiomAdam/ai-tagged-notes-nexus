using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Nexus.Desktop.ViewModels;
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
}