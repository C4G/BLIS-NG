using Avalonia.Controls;
using BLIS_NG.ViewModels;

namespace BLIS_NG.Views;

public partial class MainWindow : Window
{
  public MainWindow()
  {
    InitializeComponent();
  }

  protected override void OnClosing(WindowClosingEventArgs e)
  {
    if (DataContext is MainWindowViewModel vm)
    {
      vm.Shutdown();
    }

    base.OnClosing(e);
  }
}

