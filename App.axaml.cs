using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using BLIS_NG.Config;
using BLIS_NG.Lib;
using BLIS_NG.ViewModels;
using BLIS_NG.Views;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace BLIS_NG;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        EnvironmentChecker.CreateRequiredDirectories();

        Log.Logger = new LoggerConfiguration()
              .Enrich.FromLogContext()
              .WriteTo.Debug()
              .WriteTo.File(Path.Combine(ConfigurationFile.LOG_DIR, "blis_ng_.log"), rollingInterval: RollingInterval.Day)
              .CreateLogger();

        var collection = new ServiceCollection()
          .AddLogging(builder => builder.AddSerilog(dispose: true))
          // See Lib/ServiceCollectionExtensions.cs to see the dependency injection entrypoint.
          .AddDependencies();

        if (ApplicationLifetime != null)
        {
            collection.AddSingleton(ApplicationLifetime);
        }

        var services = collection.BuildServiceProvider();

        var vm = services.GetRequiredService<MainWindowViewModel>();
        var serverControl = services.GetRequiredService<ServerControlViewModel>();

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
