using System.Diagnostics;
using System.Globalization;
using System.Reactive;
using Avalonia.Controls.ApplicationLifetimes;
using BLIS_NG.Config;
using Avalonia.Platform.Storage;
using BLIS_NG.Server;
using Microsoft.Extensions.Logging;
using ReactiveUI;
using BLIS_NG.Lang;

namespace BLIS_NG.ViewModels;

public class LanguageOption(string code, string displayName)
{
    public string Code { get; } = code;
    public string DisplayName { get; } = displayName;
}

public class ServerControlViewModel : ViewModelBase
{
    private enum UiStatusState
    {
        Unknown,
        Healthy,
        Starting,
        ApacheHealthcheckFailed,
        Stopping,
        Stopped
    }

    private const string AppVersionNumber = "4.0";
    public static string AppVersion => string.Format(Resources.App_Version_Format, AppVersionNumber);
    public static string AppTitle => Resources.App_Title;
    public static string AppTagline => Resources.App_Tagline;
    public static string AppLicenseNotice => Resources.App_LicenseNotice;
    public static string StartBlisText => Resources.Button_StartBlis;
    public static string StopBlisText => Resources.Button_StopBlis;
    public static string MoreOptionsText => Resources.Button_MoreOptions;
    public static string UpdateWithZipFileText => Resources.Menu_UpdateWithZipFile;
    public static string ResetPasswordText => Resources.Menu_ResetPassword;
    public static string LanguageLabel => Resources.Label_Language;

    private readonly ILogger<ServerControlViewModel> logger;
    private readonly ILoggerFactory _loggerFactory;
    private readonly IMainServer mainServer;
    private readonly IClassicDesktopStyleApplicationLifetime _lifetime;
    private readonly ToolsWindowViewModel _toolsWindowViewModel;

    public ReactiveCommand<Unit, Unit> StartServerCommand { get; }
    public ReactiveCommand<Unit, Unit> StopServerCommand { get; }
    public ReactiveCommand<Unit, Unit> OpenPasswordResetCommand { get; }
    public ReactiveCommand<Unit, Unit> SelectZipCommand { get; }
    public IReadOnlyList<LanguageOption> AvailableLanguages { get; }

    public static bool SelfUpdateEnabled
    {
        get =>
#if SelfUpdateEnabled
            true;
#else
            false;
#endif
    }

    private readonly bool _initializingLanguageSelection = true;
    private UiStatusState _currentStatusState = UiStatusState.Stopped;

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

    private LanguageOption? _selectedLanguage;
    public LanguageOption? SelectedLanguage
    {
        get => _selectedLanguage;
        set
        {
            this.RaiseAndSetIfChanged(ref _selectedLanguage, value);
            if (_initializingLanguageSelection || value == null)
            {
                return;
            }

            var culture = new CultureInfo(value.Code);
            CultureInfo.DefaultThreadCurrentCulture = culture;
            CultureInfo.DefaultThreadCurrentUICulture = culture;
            CultureInfo.CurrentCulture = culture;
            CultureInfo.CurrentUICulture = culture;
            Resources.Culture = culture;
            LanguagePreferences.SaveLanguageCode(value.Code);
            RefreshLocalizedUi();
        }
    }

    public ServerControlViewModel(
        ILogger<ServerControlViewModel> logger,
        ILoggerFactory loggerFactory,
        IMainServer mainServer,
        IClassicDesktopStyleApplicationLifetime lifetime,
        ToolsWindowViewModel toolsWindowViewModel)
    {
        this.logger = logger;
        _loggerFactory = loggerFactory;
        this.mainServer = mainServer;
        _lifetime = lifetime;
        _toolsWindowViewModel = toolsWindowViewModel;

        StartServerCommand = ReactiveCommand.Create(HandleStartButtonClick);
        StopServerCommand = ReactiveCommand.Create(HandleStopButtonClick);
        OpenPasswordResetCommand = ReactiveCommand.Create(HandleOpenPasswordReset);
        SelectZipCommand = ReactiveCommand.CreateFromTask(HandleSelectZipClick);

        AvailableLanguages =
        [
            new("en", "English"),
            new("fr", "Francais"),
        ];

        var savedLanguageCode = LanguagePreferences.GetLanguageCode();
        SelectedLanguage = AvailableLanguages.FirstOrDefault(x => x.Code == savedLanguageCode) ?? AvailableLanguages[0];
        _initializingLanguageSelection = false;
        RefreshLocalizedUi();
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
            _currentStatusState = UiStatusState.Healthy;
            ApplyCurrentStatusText();
            StartBlisEnabled = false;
            StopBlisEnabled = true;
            ProbablyRunning = true;
        }
        else if (serverStatus.Apache2 == MainServer.State.Started && serverStatus.MySql == MainServer.State.Started)
        {
            _currentStatusState = UiStatusState.Starting;
            ApplyCurrentStatusText();
            StartBlisEnabled = false;
            StopBlisEnabled = false;
            ProbablyRunning = true;
        }
        else if (serverStatus.Apache2 == MainServer.State.Stopped && serverStatus.MySql == MainServer.State.Healthy)
        {
            _currentStatusState = UiStatusState.ApacheHealthcheckFailed;
            ApplyCurrentStatusText();
            StartBlisEnabled = true;
            StopBlisEnabled = false;
            ProbablyRunning = true;
        }
        else if (serverStatus.Apache2 == MainServer.State.Stopping || serverStatus.MySql == MainServer.State.Stopping)
        {
            _currentStatusState = UiStatusState.Stopping;
            ApplyCurrentStatusText();
            StartBlisEnabled = false;
            StopBlisEnabled = false;
            ProbablyRunning = true;
        }
        else
        {
            _currentStatusState = UiStatusState.Stopped;
            ApplyCurrentStatusText();
            StartBlisEnabled = true;
            StopBlisEnabled = false;
            ProbablyRunning = false;
        }
    }

    private void ApplyCurrentStatusText()
    {
        Status = _currentStatusState switch
        {
            UiStatusState.Healthy => Resources.Status_Healthy,
            UiStatusState.Starting => Resources.Status_Starting,
            UiStatusState.ApacheHealthcheckFailed => Resources.Status_ApacheHealthcheckFailed,
            UiStatusState.Stopping => Resources.Status_Stopping,
            _ => Resources.Status_Stopped,
        };
    }

    private void RefreshLocalizedUi()
    {
        this.RaisePropertyChanged(nameof(AppVersion));
        this.RaisePropertyChanged(nameof(AppTitle));
        this.RaisePropertyChanged(nameof(AppTagline));
        this.RaisePropertyChanged(nameof(AppLicenseNotice));
        this.RaisePropertyChanged(nameof(StartBlisText));
        this.RaisePropertyChanged(nameof(StopBlisText));
        this.RaisePropertyChanged(nameof(MoreOptionsText));
        this.RaisePropertyChanged(nameof(UpdateWithZipFileText));
        this.RaisePropertyChanged(nameof(ResetPasswordText));
        this.RaisePropertyChanged(nameof(LanguageLabel));
        ApplyCurrentStatusText();
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
                    Title = Resources.Picker_SelectZipFile,
                    FileTypeFilter =
                    [
                        new FilePickerFileType(Resources.Picker_ZipFiles)
                        {
                            Patterns = ["*.zip"]
                        }
                    ],
                    AllowMultiple = false
                });

                if (files.Count > 0)
                {
                    string selectedFile = files[0].Path.LocalPath;

                    // Launch the update window logic
                    var updateLogger = LoggerFactoryExtensions
                        .CreateLogger<UpdateProgressViewModel>(_loggerFactory);
                    var updateVm = new UpdateProgressViewModel(updateLogger, mainServer);
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
        _toolsWindowViewModel.PasswordResetViewModel.ResetForm();
        var toolsWindow = new Views.ToolsWindow(_toolsWindowViewModel);
        //close window action after successful reset
        _toolsWindowViewModel.PasswordResetViewModel.RequestClose = () => toolsWindow.Close();
        toolsWindow.ShowDialog(_lifetime.MainWindow);
    }
}
