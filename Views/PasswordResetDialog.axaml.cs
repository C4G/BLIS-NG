using Avalonia.Controls;
using BLIS_NG.ViewModels;

namespace BLIS_NG.Views;

public partial class PasswordResetDialog : Window
{
    public PasswordResetDialog()
    {
        InitializeComponent();
    }

    public PasswordResetDialog(PasswordResetViewModel vm)
    {
        DataContext = vm;
        InitializeComponent();
        vm.CloseDialog = () => Close();
    }

    public void SetViewModel(PasswordResetViewModel vm)
    {
        vm.CloseDialog = () => Close();
        DataContext = vm;
    }
}