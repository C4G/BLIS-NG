using Avalonia.Controls;
using BLIS_NG.ViewModels;

namespace BLIS_NG.Views;

public partial class MainWindow : Window
{
    private bool UserRequestedClose = false;

    public MainWindow()
    {
        InitializeComponent();
    }

    protected override async void OnClosing(WindowClosingEventArgs e)
    {
        if (DataContext is MainWindowViewModel vm)
        {
            var shutdownSuccess = vm.Shutdown();
            e.Cancel = !shutdownSuccess;
            if (!UserRequestedClose && !shutdownSuccess)
            {
                UserRequestedClose = true;
                await Task.Run(async () =>
                {
                    await Task.Delay(5000);
                    vm.TryShutdown();
                });
            }
        }

        base.OnClosing(e);
    }
}

