using System.Diagnostics;
using System.Reactive;
using ReactiveUI;

namespace BLIS_NG.ViewModels;

public class ServerControlViewModel
{
  public string AppVersion { get { return "4.0"; } }

  public ReactiveCommand<Unit, Unit> StartServerCommand { get; }

  public ServerControlViewModel()
  {
    StartServerCommand = ReactiveCommand.Create(HandleButtonClick);
  }

  public void HandleButtonClick()
  {
    Debug.WriteLine("Server Started");
  }
}

