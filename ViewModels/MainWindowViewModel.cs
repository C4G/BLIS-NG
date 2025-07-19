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

    Thread.Sleep(3000);

    WindowState = WindowState.Minimized;
  }
}
