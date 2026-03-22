using System.Diagnostics;
using System.Reactive;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using BLIS_NG.Server;
using BLIS_NG.Config;
using Microsoft.Extensions.Logging;
using ReactiveUI;
using System.IO;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace BLIS_NG.ViewModels;

public class ServerControlViewModel : ViewModelBase
{
    private const string AppVersionNumber = "4.0";
    public static string AppVersion
    {
        get { return $"BLIS for Windows {AppVersionNumber}"; }
    }

    private readonly ILogger<ServerControlViewModel> logger;
    private readonly IMainServer mainServer;
    private readonly IClassicDesktopStyleApplicationLifetime _lifetime;
    private readonly MySqlAdmin _mySqlAdmin;

    public ReactiveCommand<Unit, Unit> StartServerCommand { get; }
    public ReactiveCommand<Unit, Unit> StopServerCommand { get; }
    public ReactiveCommand<Unit, Unit> OpenPasswordResetCommand { get; }
    public ReactiveCommand<Unit, Unit> SelectZipCommand { get; }

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

    public ServerControlViewModel(
        ILogger<ServerControlViewModel> logger,
        IMainServer mainServer,
        IClassicDesktopStyleApplicationLifetime lifetime,
        MySqlAdmin mySqlAdmin)
    {
        this.logger = logger;
        this.mainServer = mainServer;
        _lifetime = lifetime;
        _mySqlAdmin = mySqlAdmin;

        StartServerCommand = ReactiveCommand.Create(HandleStartButtonClick);
        StopServerCommand = ReactiveCommand.Create(HandleStopButtonClick);
        OpenPasswordResetCommand = ReactiveCommand.Create(HandleOpenPasswordReset);
        SelectZipCommand = ReactiveCommand.CreateFromTask(HandleSelectZipClick);
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
        if (StopBlisEnabled)
            await mainServer.Stop();
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

    private async Task HandleSelectZipClick()
    {
        if (Avalonia.Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
            && desktop.MainWindow != null)
        {
            var topLevel = Avalonia.Controls.TopLevel.GetTopLevel(desktop.MainWindow);
            if (topLevel != null)
            {
                var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
                {
                    Title = "Select ZIP File",
                    FileTypeFilter = new[]
                    {
                        new FilePickerFileType("ZIP Files")
                        {
                            Patterns = new[] { "*.zip" }
                        }
                    },
                    AllowMultiple = false
                });

                if (files.Count > 0)
                {
                    string selectedFile = files[0].Path.LocalPath;

                    // Launch the update window logic
                    var updateVm = new UpdateProgressViewModel();
                    var updateWindow = new Views.UpdateProgressWindow
                    {
                        DataContext = updateVm
                    };

                    updateWindow.Show(desktop.MainWindow);

                    // Start the update process and close window when done
                    await updateVm.StartUpdate(selectedFile, () => updateWindow.Close());
                }
            }
        }
    }

    public void OnExit(object? sender, ControlledApplicationLifetimeExitEventArgs e)
    {
        HandleStopButtonClick();
    }

    private void HandleOpenPasswordReset()
    {
        if (_lifetime.MainWindow is null) return;
        var viewModel = new ToolsWindowViewModel(_mySqlAdmin);
        var toolsWindow = new BLIS_NG.Views.ToolsWindow(viewModel);
        toolsWindow.ShowDialog(_lifetime.MainWindow);
    }
}
