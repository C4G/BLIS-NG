using System.Reactive;
using BLIS_NG.server;
using Microsoft.Extensions.Logging;
using ReactiveUI;

namespace BLIS_NG.ViewModels;

public class ServerControlViewModel
{
  public static string AppVersion { get { return "4.0"; } }

  private readonly MySqlServer mySqlServer;
  private Task? mysqlServerTask;

  public ReactiveCommand<Unit, Unit> StartServerCommand { get; }
  public ReactiveCommand<Unit, Unit> StopServerCommand { get; }

  public ServerControlViewModel(ILoggerFactory loggerFactory)
  {
    mySqlServer = new(loggerFactory);

    StartServerCommand = ReactiveCommand.Create(HandleStartButtonClick);
    StopServerCommand = ReactiveCommand.Create(HandleStopButtonClick);
  }

  public void HandleStartButtonClick()
  {
    if (mysqlServerTask == null && !mySqlServer.IsRunning)
    {
      mysqlServerTask = mySqlServer.Run();
    }
  }

  public async void HandleStopButtonClick()
  {
    // TODO: Need some kind of timeout here

    if (mysqlServerTask != null)
    {
      mySqlServer.Stop();
      await mysqlServerTask;
      mysqlServerTask = null;
    }
  }
}

