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

        // Start BLIS on app start
        ServerControlViewModel.HandleStartButtonClick();
        WindowState = WindowState.Minimized;
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
