using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Nexus.Desktop.ViewModels;
using System;

namespace Nexus.Desktop.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    protected override async void OnOpened(EventArgs e)
    {
        base.OnOpened(e);
        if (DataContext is MainViewModel vm)
            await vm.InitializeAsync();
    }
}