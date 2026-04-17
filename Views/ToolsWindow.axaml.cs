using Avalonia.Controls;
using BLIS_NG.ViewModels;

namespace BLIS_NG.Views;

public partial class ToolsWindow : Window
{
    public ToolsWindow()
    {
        InitializeComponent();
    }

    public ToolsWindow(ToolsWindowViewModel vm)
    {
        DataContext = vm;
        InitializeComponent();
    }
}
