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
        // 1️⃣ Build the service collection
        var sc = new ServiceCollection();

        // 2️⃣ Register Core / Data services
        sc.AddSingleton<DbContext>(sp =>
        {
            var ctx = new DbContext("notes.db");
            ctx.Initialize();
            return ctx;
        });
        sc.AddSingleton<INoteRepository, NoteRepository>();

        // 3️⃣ Register ViewModels
        sc.AddTransient<MainViewModel>();

        // 4️⃣ Register Windows
        sc.AddSingleton<MainWindow>();

        // 5️⃣ Build provider
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