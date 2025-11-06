using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Nexus.Desktop;

public partial class App : Application
{
    public static IServiceProvider Services { get; private set; }
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        var sc = new ServiceCollection();
        // services (interfaces -> implementation)
        // sc.AddSingleton(INoteStore, SqliteNoteStore>();  // repo/store
        // sc.AddSingleton(INoteService, NoteService>();  // business logic

        // view-models
        // sc.AddTransient<MainViewModel>();

        // windows
        sc.AddSingleton<MainWindow>();

        Services = sc.BuildServiceProvider();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var main = Services.GetRequiredService<MainWindow>();
            main.DataContext = Services.GetRequiredService<MainWindow>();
            desktop.MainWindow = main;
        }

        base.OnFrameworkInitializationCompleted();
    }
}