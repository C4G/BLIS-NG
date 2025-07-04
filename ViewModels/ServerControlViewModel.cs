using System.Reactive;
using Avalonia.Logging;
using BLIS_NG.Config;
using BLIS_NG.server;
using Microsoft.Extensions.Logging;
using ReactiveUI;

namespace BLIS_NG.ViewModels;

public class ServerControlViewModel
{
  public static string AppVersion { get { return "4.0"; } }

  private readonly ILogger<ServerControlViewModel> logger;
  private readonly MySqlServer mySqlServer;
  private Task? mysqlServerTask;

  public ReactiveCommand<Unit, Unit> StartServerCommand { get; }
  public ReactiveCommand<Unit, Unit> StopServerCommand { get; }

  public string ServerLog { get; set; } = "";

  public ServerControlViewModel()
  {
    var loggerFactory = AppConfig.CreateLoggerFactory();
    mySqlServer = new(loggerFactory);
    logger = loggerFactory.CreateLogger<ServerControlViewModel>();

    StartServerCommand = ReactiveCommand.Create(HandleStartButtonClick);
    StopServerCommand = ReactiveCommand.Create(HandleStopButtonClick);
  }

  public void HandleStartButtonClick()
  {
    if (mysqlServerTask == null && !mySqlServer.IsRunning)
    {
      mysqlServerTask = mySqlServer.Run(
        (stdout) =>
        {
          logger.LogInformation("{}", stdout);
          ServerLog += stdout;
        },
        (stderr) =>
        {
          logger.LogError("{}", stderr);
          ServerLog += stderr;
        }
      );
    }
  }

  public async void HandleStopButtonClick()
  {
    // TODO: Need some kind of timeout here

    mySqlServer.Stop();
    if (mysqlServerTask != null)
    {
      await mysqlServerTask;
      mysqlServerTask = null;
    }
  }
}

