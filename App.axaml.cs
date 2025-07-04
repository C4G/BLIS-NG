using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using BLIS_NG.ViewModels;
using BLIS_NG.Views;
using Microsoft.Extensions.Logging;

namespace BLIS_NG;

public partial class App : Application
{
  private readonly ILoggerFactory loggerFactory = LoggerFactory.Create((builder) =>
    builder.AddDebug());

  public override void Initialize()
  {
    AvaloniaXamlLoader.Load(this);
  }

  public override void OnFrameworkInitializationCompleted()
  {
    if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
    {
      desktop.MainWindow = new MainWindow
      {
        DataContext = new MainWindowViewModel(loggerFactory),
      };
    }

    base.OnFrameworkInitializationCompleted();
  }
}
