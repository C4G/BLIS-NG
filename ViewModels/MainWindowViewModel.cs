using Microsoft.Extensions.Logging;

namespace BLIS_NG.ViewModels
{
  public class MainWindowViewModel(ILoggerFactory loggerFactory) : ViewModelBase
  {
    public ServerControlViewModel ServerControlViewModel { get; } = new ServerControlViewModel(loggerFactory);
  }
}
