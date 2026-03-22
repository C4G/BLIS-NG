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
        // "Reset Form" clears fields rather than closing the window
        vm.PasswordResetViewModel.CloseDialog = () => vm.PasswordResetViewModel.ResetForm();
    }
}
