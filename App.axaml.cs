using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using BLIS_NG.Config;
using BLIS_NG.Lib;
using BLIS_NG.Lang;
using BLIS_NG.ViewModels;
using BLIS_NG.Views;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using System.Globalization;

namespace BLIS_NG;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        var selectedLanguageCode = LanguagePreferences.GetLanguageCode();
        var appCulture = new CultureInfo(selectedLanguageCode);
        CultureInfo.DefaultThreadCurrentCulture = appCulture;
        CultureInfo.DefaultThreadCurrentUICulture = appCulture;
        CultureInfo.CurrentCulture = appCulture;
        CultureInfo.CurrentUICulture = appCulture;
        BLIS_NG.Lang.Resources.Culture = appCulture;

        Log.Logger = new LoggerConfiguration()
            .Enrich.FromLogContext()
            .WriteTo.Debug()
            .WriteTo.File(Path.Combine(ConfigurationFile.ResolveBaseDirectory(), "log", "blis_ng_.log"), rollingInterval: RollingInterval.Day)
            .CreateLogger();

        // Clean up leftover artifacts from a previous self-update (old exe + staging dir)
        UpdateProgressViewModel.StartupCleanup();

        var collection = new ServiceCollection()
            .AddLogging(builder => builder.AddSerilog(dispose: true))
            // See Lib/ServiceCollectionExtensions.cs to see the dependency injection entrypoint.
            .AddDependencies();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktopLifetime)
        {
            collection.AddSingleton(ApplicationLifetime);
            collection.AddSingleton(desktopLifetime);
        }

        var services = collection.BuildServiceProvider();

        var vm = services.GetRequiredService<MainWindowViewModel>();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow
            {
                DataContext = vm,
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}
