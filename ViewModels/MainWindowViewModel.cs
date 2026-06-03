using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using BLIS_NG.Config;

namespace BLIS_NG.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    public static WindowState WindowState { get; set; }

    public ServerControlViewModel ServerControlViewModel { get; init; }

    private IApplicationLifetime? ApplicationLifetime { get; init; }
    private readonly AppSettings appSettings;

    public MainWindowViewModel(IApplicationLifetime? lifetime, ServerControlViewModel serverControlViewModel, AppSettings appSettings)
    {
        ApplicationLifetime = lifetime;
        this.appSettings = appSettings;
        ServerControlViewModel = serverControlViewModel;

        // BLIS doesn't (yet) run on non-Windows platforms,
        // so don't attempt to open it in the browser if we're not
        // on Windows.
        if (appSettings.OpenBrowserOnStart && OperatingSystem.IsWindows())
        {
            // Start BLIS on app start
            ServerControlViewModel.HandleStartButtonClick();

            // macOS implementation note: if the application starts minimized,
            // the launcher window will fail to render properly.
            // So if this is ever enabled for macOS... don't start minimized.
            WindowState = WindowState.Minimized;
        }
    }

    public bool Shutdown()
    {
        appSettings.Write();
        ServerControlViewModel.HandleStopButtonClick();
        return !ServerControlViewModel.ProbablyRunning;
    }

    public void TryShutdown()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime lifetime)
        {
            lifetime.TryShutdown();
        }
    }
}
