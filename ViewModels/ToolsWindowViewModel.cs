namespace BLIS_NG.ViewModels;

public class ToolsWindowViewModel(PasswordResetViewModel passwordResetViewModel) : ViewModelBase
{
    public PasswordResetViewModel PasswordResetViewModel { get; } = passwordResetViewModel;
}
