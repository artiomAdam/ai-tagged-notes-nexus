using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using System;
using Nexus.Core.Storage;
using Nexus.Core.Interfaces;
using Nexus.Desktop.ViewModels;
using Nexus.Desktop.Views;
using Nexus.Core.Services;

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

        // Database & Repositories
        sc.AddSingleton<DbContext>(sp =>
        {
            var ctx = new DbContext();
            ctx.Initialize();
            return ctx;
        });

        sc.AddSingleton<INoteRepository, NoteRepository>();
        sc.AddSingleton<ITopicsRepository, TopicsRepository>();
        sc.AddSingleton<INoteTopicsRepository, NoteTopicsRepository>();

        // ViewModels
        sc.AddTransient<MainViewModel>();
        sc.AddTransient<NoteEditorViewModel>();

        // Windows
        sc.AddSingleton<MainWindow>();
        sc.AddSingleton<NoteEditorView>();

        // Services
        sc.AddSingleton<TagPredictor>();

        Services = sc.BuildServiceProvider();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var main = Services.GetRequiredService<MainWindow>();
            main.DataContext = Services.GetRequiredService<MainViewModel>();
            desktop.MainWindow = main;
        }

        base.OnFrameworkInitializationCompleted();
    }
}