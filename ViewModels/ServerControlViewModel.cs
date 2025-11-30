using System.Diagnostics;
using System.Reactive;
using Avalonia.Controls.ApplicationLifetimes;
using BLIS_NG.Server;
using Microsoft.Extensions.Logging;
using ReactiveUI;

namespace BLIS_NG.ViewModels;

public class ServerControlViewModel : ViewModelBase
{
    private const string AppVersionNumber = "4.0";
    public static string AppVersion { get { return $"BLIS for Windows {AppVersionNumber}"; } }

    private readonly ILogger<ServerControlViewModel> logger;
    private readonly IMainServer mainServer;

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

    public bool ProbablyRunning { get; private set; }

    public ServerControlViewModel(ILogger<ServerControlViewModel> logger, IMainServer mainServer)
    {
        this.logger = logger;
        this.mainServer = mainServer;

        StartServerCommand = ReactiveCommand.Create(HandleStartButtonClick);
        StopServerCommand = ReactiveCommand.Create(HandleStopButtonClick);
    }

    public void HandleStartButtonClick()
    {
        mainServer.Start(HealthcheckAndUpdateStatus);

        StartBlisEnabled = false;
        StopBlisEnabled = true;

        Thread.Sleep(1000);
        OpenUrl(MainServer.ServerUri);
    }

    public async void HandleStopButtonClick()
    {
        if (StopBlisEnabled) {
            await mainServer.Stop();
        }
    }

    private void HealthcheckAndUpdateStatus(MainServer.ServerStatus serverStatus)
    {
        if (serverStatus.Apache2 == MainServer.State.Healthy && serverStatus.MySql == MainServer.State.Healthy)
        {
            Status = "Status: Healthy";
            StartBlisEnabled = false;
            StopBlisEnabled = true;
            ProbablyRunning = true;
        }
        else if (serverStatus.Apache2 == MainServer.State.Started && serverStatus.MySql == MainServer.State.Started)
        {
            Status = "Status: Starting";
            StartBlisEnabled = false;
            StopBlisEnabled = false;
            ProbablyRunning = true;
        }
        else if (serverStatus.Apache2 == MainServer.State.Stopped && serverStatus.MySql == MainServer.State.Healthy)
        {
            Status = "Status: Apache2 health check failed.";
            StartBlisEnabled = true;
            StopBlisEnabled = false;
            ProbablyRunning = true;
        } 
        else if (serverStatus.Apache2 == MainServer.State.Stopping || serverStatus.MySql == MainServer.State.Stopping)
        {
            Status = "Status: Stopping";
            StartBlisEnabled = false;
            StopBlisEnabled = false;
            ProbablyRunning = true;
        }
        else
        {
            Status = "Status: Stopped";
            StartBlisEnabled = true;
            StopBlisEnabled = false;
            ProbablyRunning = false;
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

