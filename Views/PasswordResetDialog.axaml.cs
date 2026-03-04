using Avalonia.Controls;
using BLIS_NG.ViewModels;

namespace BLIS_NG.Views;

public partial class PasswordResetDialog : Window
{
    public PasswordResetDialog()
    {
        InitializeComponent();

        var vm = new PasswordResetViewModel();
        vm.CloseDialog = () => Close();
        DataContext = vm;
    }
}
