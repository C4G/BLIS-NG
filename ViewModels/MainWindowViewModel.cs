namespace BLIS_NG.ViewModels
{
  public class MainWindowViewModel() : ViewModelBase
  {
    public static string WindowTitle { get => ServerControlViewModel.AppVersion; }

    public ServerControlViewModel ServerControlViewModel { get; } = new ServerControlViewModel();
  }
}
