using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;

namespace BLIS_NG.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    public static string WindowTitle { get => ServerControlViewModel.AppVersion; }

    public static WindowState WindowState { get; set; }

    public ServerControlViewModel ServerControlViewModel { get; init; }

    private IApplicationLifetime? ApplicationLifetime { get; init; }

    public MainWindowViewModel(IApplicationLifetime? lifetime, ServerControlViewModel serverControlViewModel)
    {
        ApplicationLifetime = lifetime;
        ServerControlViewModel = serverControlViewModel;

        // BLIS doesn't (yet) run on non-Windows platforms,
        // so don't attempt to open it in the browser if we're not
        // on Windows.
        if (OperatingSystem.IsWindows())
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
