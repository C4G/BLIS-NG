namespace BLIS_NG.ViewModels;

public class MainWindowViewModel(ServerControlViewModel serverControlViewModel) : ViewModelBase
{
  public static string WindowTitle { get => ServerControlViewModel.AppVersion; }

  public ServerControlViewModel ServerControlViewModel { get; } = serverControlViewModel;
}
