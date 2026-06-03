using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using BLIS_NG.Config;
using BLIS_NG.Lib;
using BLIS_NG.ViewModels;
using BLIS_NG.Views;
using Microsoft.Extensions.Configuration;
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
        IConfiguration configuration = new ConfigurationBuilder()
            .AddJsonFile(AppSettings.ConfigPath())
            .AddEnvironmentVariables()
            .AddCommandLine(Environment.GetCommandLineArgs())
            .Build();

        AppSettings appSettings = configuration.GetSection(AppSettings.LauncherSettings).Get<AppSettings>() ?? new AppSettings();

        var selectedLanguageCode = appSettings.Language.LanguageCode();
        var appCulture = new CultureInfo(selectedLanguageCode);
        CultureInfo.DefaultThreadCurrentCulture = appCulture;
        CultureInfo.DefaultThreadCurrentUICulture = appCulture;
        CultureInfo.CurrentCulture = appCulture;
        CultureInfo.CurrentUICulture = appCulture;
        Lang.Resources.Culture = appCulture;

        Log.Logger = new LoggerConfiguration()
            .Enrich.FromLogContext()
            .WriteTo.Debug()
            .WriteTo.File(Path.Join(AppSettings.ResolveBaseDirectory(), "log", "blis_ng_.log"), rollingInterval: RollingInterval.Day)
            .CreateLogger();

        // Clean up leftover artifacts from a previous self-update (old exe + staging dir)
        UpdateProgressViewModel.StartupCleanup();

        var collection = new ServiceCollection()
            .AddLogging(builder => builder.AddSerilog(dispose: true))
            .AddSingleton(appSettings)
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
