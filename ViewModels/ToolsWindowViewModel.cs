using BLIS_NG.Server;

namespace BLIS_NG.ViewModels;

public class ToolsWindowViewModel : ViewModelBase
{
    public PasswordResetViewModel PasswordResetViewModel { get; }

    // Future: public UpdateViewModel UpdateViewModel { get; }

    public ToolsWindowViewModel(MySqlAdmin mySqlAdmin)
    {
        PasswordResetViewModel = new PasswordResetViewModel(mySqlAdmin);
    }
}
