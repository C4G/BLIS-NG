using Avalonia.Controls;

namespace BLIS_NG.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
  public static string WindowTitle { get => ServerControlViewModel.AppVersion; }

  public static WindowState WindowState { get; set; }

  public ServerControlViewModel ServerControlViewModel { get; init; }

  public MainWindowViewModel(ServerControlViewModel serverControlViewModel)
  {
    ServerControlViewModel = serverControlViewModel;

    // Start BLIS on app start
    ServerControlViewModel.HandleStartButtonClick();
    WindowState = WindowState.Minimized;
  }

  public void Shutdown()
  {
    // Run method synchronously and wait for result.
    var awaiter = Task.Run(ServerControlViewModel.HandleStopButtonClick).GetAwaiter();
    // Wait a little while for things to shutdown cleanly
    Thread.Sleep(5000);
    awaiter.GetResult();
  }
}
