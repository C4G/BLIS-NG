using System.Reactive;
using BLIS_NG.Config;
using BLIS_NG.Server;
using Microsoft.Extensions.Logging;
using ReactiveUI;

namespace BLIS_NG.ViewModels;

public class ServerControlViewModel : ViewModelBase
{
  private const string AppVersionNumber = "4.0";
  public static string AppVersion { get { return $"BLIS for Windows {AppVersionNumber}"; } }

  private readonly ILogger<ServerControlViewModel> logger;

  private readonly MySqlAdmin mySqlAdmin;
  private readonly MySqlServer mySqlServer;
  private Task? mysqlServerTask;

  private readonly Apache2Server apache2Server;
  private Task? apacheServerTask;

  private CancellationTokenSource healthcheckCanceler = new();
  private Task? healthcheck;

  public ReactiveCommand<Unit, Unit> StartServerCommand { get; }
  public ReactiveCommand<Unit, Unit> StopServerCommand { get; }

  private string _status = string.Empty;
  public string Status
  {
    get => _status;
    set => this.RaiseAndSetIfChanged(ref _status, value);
  }

  private bool _startBlisEnabled = true;
  public bool StartBlisEnabled
  {
    get => _startBlisEnabled;
    set => this.RaiseAndSetIfChanged(ref _startBlisEnabled, value);
  }

  private bool _stopBlisEnabled = false;
  public bool StopBlisEnabled
  {
    get => _stopBlisEnabled;
    set => this.RaiseAndSetIfChanged(ref _stopBlisEnabled, value);
  }

  public ServerControlViewModel(ILogger<ServerControlViewModel> logger, MySqlServer mySqlServer, MySqlAdmin mySqlAdmin, Apache2Server apache2Server)
  {
    this.mySqlServer = mySqlServer;
    this.mySqlAdmin = mySqlAdmin;
    this.apache2Server = apache2Server;
    this.logger = logger;

    StartServerCommand = ReactiveCommand.Create(HandleStartButtonClick);
    StopServerCommand = ReactiveCommand.Create(HandleStopButtonClick);
  }

  public void HandleStartButtonClick()
  {
    if (mysqlServerTask == null && !mySqlServer.IsRunning)
    {
      mysqlServerTask = mySqlServer.Run();
    }

    if (apacheServerTask == null && !apache2Server.IsRunning)
    {
      apacheServerTask = apache2Server.Run();
    }

    healthcheck ??= Task.Run(async () =>
      {
        while (!healthcheckCanceler.IsCancellationRequested)
        {
          await HealthcheckAndUpdateStatus();
          Thread.Sleep(1000);
        }
      });

    StartBlisEnabled = false;
    StopBlisEnabled = true;
  }

  public async void HandleStopButtonClick()
  {
    // TODO: Need some kind of timeout here

    if (healthcheck != null)
    {
      healthcheckCanceler.Cancel();
      await healthcheck;
      healthcheckCanceler = new();
      healthcheck = null;
    }

    apache2Server.Stop();
    mySqlServer.Stop();

    if (apacheServerTask != null)
    {
      await apacheServerTask;
      apacheServerTask = null;
    }

    if (mysqlServerTask != null)
    {
      await mysqlServerTask;
      mysqlServerTask = null;
    }

    StartBlisEnabled = true;
    StopBlisEnabled = false;
    await HealthcheckAndUpdateStatus();
  }

  private async Task HealthcheckAndUpdateStatus()
  {
    var mysqlUp = await mySqlAdmin.Ping();
    if (mysqlUp)
    {
      Status = "Status: Healthy";
    }
    else
    {
      Status = "Status: Stopped";
    }
  }
}

