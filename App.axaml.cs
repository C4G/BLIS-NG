using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using BLIS_NG.ViewModels;
using BLIS_NG.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
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
    Log.Logger = new LoggerConfiguration()
          .Enrich.FromLogContext()
          .WriteTo.Debug()
          .WriteTo.File("blis_ng_.log", rollingInterval: RollingInterval.Day)
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
}
