using System.Reactive;
using BLIS_NG.Config;
using BLIS_NG.Server;
using Microsoft.Extensions.Logging;
using ReactiveUI;

namespace BLIS_NG.ViewModels;

public class ServerControlViewModel
{
  private const string AppVersionNumber = "4.0";
  public static string AppVersion { get { return $"BLIS for Windows {AppVersionNumber}"; } }

  private readonly ILogger<ServerControlViewModel> logger;
  private readonly MySqlServer mySqlServer;
  private Task? mysqlServerTask;

  private readonly Apache2Server apache2Server;
  private Task? apacheServerTask;

  public ReactiveCommand<Unit, Unit> StartServerCommand { get; }
  public ReactiveCommand<Unit, Unit> StopServerCommand { get; }

  public string ServerLog { get; set; } = "";

  public ServerControlViewModel(ILogger<ServerControlViewModel> logger, MySqlServer mySqlServer, Apache2Server apache2Server)
  {
    this.mySqlServer = mySqlServer;
    this.apache2Server = apache2Server;
    this.logger = logger;

    StartServerCommand = ReactiveCommand.Create(HandleStartButtonClick);
    StopServerCommand = ReactiveCommand.Create(HandleStopButtonClick);
  }

  public void HandleStartButtonClick()
  {
    ConfigurationFile.MakeRequiredDirectories();

    if (mysqlServerTask == null && !mySqlServer.IsRunning)
    {
      mysqlServerTask = mySqlServer.Run();
    }

    if (apacheServerTask == null && !apache2Server.IsRunning)
    {
      apacheServerTask = apache2Server.Run();
    }
  }

  public async void HandleStopButtonClick()
  {
    // TODO: Need some kind of timeout here

    apache2Server.Stop();
    if (apacheServerTask != null)
    {
      await apacheServerTask;
      apacheServerTask = null;
    }

    mySqlServer.Stop();
    if (mysqlServerTask != null)
    {
      await mysqlServerTask;
      mysqlServerTask = null;
    }
  }
}

