using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using System;
using Nexus.Core.Storage;
using Nexus.Core.Interfaces;
using Nexus.Desktop.ViewModels;
using Nexus.Desktop.Views;

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
        // Build the service collection
        var sc = new ServiceCollection();

        // Register Core / Data services
        sc.AddSingleton<DbContext>(sp =>
        {
            var ctx = new DbContext();
            ctx.Initialize();
            return ctx;
        });
        sc.AddSingleton<INoteRepository, NoteRepository>();

        // Register ViewModels
        sc.AddTransient<MainViewModel>();
        sc.AddTransient<NoteEditorViewModel>();

        // Register Windows
        sc.AddSingleton<MainWindow>();
        sc.AddSingleton<NoteEditorView>();

        // Build provider
        Services = sc.BuildServiceProvider();

        // 6️⃣ Launch main window
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var main = Services.GetRequiredService<MainWindow>();
            main.DataContext = Services.GetRequiredService<MainViewModel>();
            desktop.MainWindow = main;
        }

        base.OnFrameworkInitializationCompleted();
    }
}