using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using BLIS_NG.ViewModels;
using BLIS_NG.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace BLIS_NG;

public partial class App : Application
{
  public override void Initialize()
  {
    AvaloniaXamlLoader.Load(this);
  }

  public override void OnFrameworkInitializationCompleted()
  {
    var collection = new ServiceCollection();
    collection.AddLogging(builder => builder.AddDebug());
    collection.AddSingleton<ServerControlViewModel>();
    collection.AddSingleton<MainWindowViewModel>();

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
