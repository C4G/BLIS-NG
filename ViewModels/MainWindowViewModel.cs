namespace BLIS_NG.ViewModels
{
  public class MainWindowViewModel : ViewModelBase
  {
    public ServerControlViewModel ServerControlViewModel { get; } = new ServerControlViewModel();
  }
}
