using System.Diagnostics;
using System.Reactive;
using Avalonia.Controls.ApplicationLifetimes;
using BLIS_NG.Config;
using BLIS_NG.Lib;
using BLIS_NG.Server;
using Microsoft.Extensions.Logging;
using ReactiveUI;

namespace BLIS_NG.ViewModels;

public class ServerControlViewModel : ViewModelBase
{
  private const string AppVersionNumber = "4.0";
  public static string AppVersion { get { return $"BLIS for Windows {AppVersionNumber}"; } }

  private readonly ILogger<ServerControlViewModel> logger;

  private readonly MySqlServer mySqlServer;
  private Task? mysqlServerTask;

  private readonly Apache2Server apache2Server;
  private Task? apacheServerTask;

  private readonly HealthcheckService healthcheckService;
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

  public ServerControlViewModel(ILogger<ServerControlViewModel> logger, MySqlServer mySqlServer, Apache2Server apache2Server, HealthcheckService healthcheckService)
  {
    this.mySqlServer = mySqlServer;
    this.apache2Server = apache2Server;
    this.logger = logger;
    this.healthcheckService = healthcheckService;

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

    Thread.Sleep(1000);
    OpenUrl(new Uri($"http://127.0.0.1:{HttpdConf.APACHE2_PORT}/"));
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
    var mysqlUp = await healthcheckService.MySqlHealthy();
    var apache2up = await healthcheckService.Apache2Healthy();

    if (mysqlUp && apache2up)
    {
      Status = "Status: Healthy";
    }
    else if (mysqlUp && !apache2up)
    {
      Status = "Status: Apache2 health check failed.";
    }
    else
    {
      Status = "Status: Stopped";
    }
  }

  private void OpenUrl(Uri url)
  {
    try
    {
      Process.Start(new ProcessStartInfo { FileName = url.ToString(), UseShellExecute = true });
    }
    catch (Exception e)
    {
      logger.LogError(e, "Could not open URL in browser: {Url}", url);
    }
  }
  
  public void OnExit(object? sender, ControlledApplicationLifetimeExitEventArgs e)
  {
    // Shutdown server when closing
    HandleStopButtonClick();
  }
}

