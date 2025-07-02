using System.Diagnostics;
using System.Reactive;
using BLIS_NG.server;
using ReactiveUI;

namespace BLIS_NG.ViewModels;

public class ServerControlViewModel
{
  public string AppVersion { get { return "4.0"; } }

  public MySqlServer mySqlServer = new();

  public ReactiveCommand<Unit, Unit> StartServerCommand { get; }

  public ServerControlViewModel()
  {
    StartServerCommand = ReactiveCommand.Create(HandleButtonClick);
  }

  public void HandleButtonClick()
  {
    if (!mySqlServer.IsRunning)
    {
      mySqlServer.Run((s) => Debug.WriteLine(s), (e) => Debug.WriteLine(e));
    }
    else
    {
      Debug.WriteLine("Server is already running.");
    }
  }
}

