using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using BLIS_NG.Config;
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
    MakeRequiredDirectories();

    Log.Logger = new LoggerConfiguration()
          .Enrich.FromLogContext()
          .WriteTo.Debug()
          .WriteTo.File(Path.Combine(ConfigurationFile.LOG_DIR, "blis_ng_.log"), rollingInterval: RollingInterval.Day)
          .CreateLogger();

    var collection = new ServiceCollection()
      .AddLogging(builder => builder.AddSerilog(dispose: true))
      .AddDependencies();

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

  private static void MakeRequiredDirectories()
  {
    Directory.CreateDirectory(PhpIni.PHP_SESSION_PATH);
    Directory.CreateDirectory(ConfigurationFile.TMP_DIR);
    Directory.CreateDirectory(ConfigurationFile.LOG_DIR);
  }
}
