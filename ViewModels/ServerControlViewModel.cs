using System.Diagnostics;
using System.Globalization;
using System.Reactive;
using Avalonia.Controls.ApplicationLifetimes;
using BLIS_NG.Config;
using BLIS_NG.Server;
using Microsoft.Extensions.Logging;
using ReactiveUI;
using BLIS_NG.Lang;

namespace BLIS_NG.ViewModels;

public class LanguageOption
{
    public string Code { get; }
    public string DisplayName { get; }

    public LanguageOption(string code, string displayName)
    {
        Code = code;
        DisplayName = displayName;
    }
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
    public string AppVersion => string.Format(Resources.App_Version_Format, AppVersionNumber);
    public string AppTitle => Resources.App_Title;
    public string AppTagline => Resources.App_Tagline;
    public string AppLicenseNotice => Resources.App_LicenseNotice;
    public string StartBlisText => Resources.Button_StartBlis;
    public string StopBlisText => Resources.Button_StopBlis;
    public string LanguageLabel => Resources.Label_Language;

    private readonly ILogger<ServerControlViewModel> logger;
    private readonly IMainServer mainServer;

    public ReactiveCommand<Unit, Unit> StartServerCommand { get; }
    public ReactiveCommand<Unit, Unit> StopServerCommand { get; }
    public IReadOnlyList<LanguageOption> AvailableLanguages { get; }

    private bool _initializingLanguageSelection = true;
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

    public ServerControlViewModel(ILogger<ServerControlViewModel> logger, IMainServer mainServer)
    {
        this.logger = logger;
        this.mainServer = mainServer;

        StartServerCommand = ReactiveCommand.Create(HandleStartButtonClick);
        StopServerCommand = ReactiveCommand.Create(HandleStopButtonClick);

        AvailableLanguages = new List<LanguageOption>
        {
            new("en", "English"),
            new("fr", "Francais"),
        };

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
        {
            await mainServer.Stop();
        }
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

    public void OnExit(object? sender, ControlledApplicationLifetimeExitEventArgs e)
    {
        // Shutdown server when closing
        HandleStopButtonClick();
    }
}
