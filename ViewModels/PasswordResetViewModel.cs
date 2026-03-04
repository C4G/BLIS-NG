using System.Reactive;
using ReactiveUI;

namespace BLIS_NG.ViewModels;

public class PasswordResetViewModel : ViewModelBase
{
    public ReactiveCommand<Unit, Unit> ResetPasswordCommand { get; }
    public ReactiveCommand<Unit, Unit> CancelCommand { get; }

    private string _username = string.Empty;
    public string Username
    {
        get => _username;
        set => this.RaiseAndSetIfChanged(ref _username, value);
    }

    private string _newPassword = string.Empty;
    public string NewPassword
    {
        get => _newPassword;
        set => this.RaiseAndSetIfChanged(ref _newPassword, value);
    }

    private string _confirmPassword = string.Empty;
    public string ConfirmPassword
    {
        get => _confirmPassword;
        set => this.RaiseAndSetIfChanged(ref _confirmPassword, value);
    }

    private string _errorMessage = string.Empty;
    public string ErrorMessage
    {
        get => _errorMessage;
        set
        {
            this.RaiseAndSetIfChanged(ref _errorMessage, value);
            this.RaisePropertyChanged(nameof(HasError));
        }
    }

    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

    // Callback so the dialog can close itself (set from the View's code-behind)
    public Action? CloseDialog { get; set; }

    public PasswordResetViewModel()
    {
        ResetPasswordCommand = ReactiveCommand.Create(HandleReset);
        CancelCommand = ReactiveCommand.Create(HandleCancel);
    }

    private string _successMessage = string.Empty;
    public string SuccessMessage
    {
        get => _successMessage;
        set
        {
            this.RaiseAndSetIfChanged(ref _successMessage, value);
            this.RaisePropertyChanged(nameof(HasSuccess));
        }
    }

    public bool HasSuccess => !string.IsNullOrEmpty(SuccessMessage);


    private void HandleReset()
    {
        // Basic validation
        if (string.IsNullOrWhiteSpace(Username))
        {
            ErrorMessage = "Username is required.";
            return;
        }
        if (string.IsNullOrWhiteSpace(NewPassword))
        {
            ErrorMessage = "New password is required.";
            return;
        }
        if (NewPassword != ConfirmPassword)
        {
            ErrorMessage = "Passwords do not match.";
            return;
        }

        ErrorMessage = string.Empty;

        // TODO: wire up SHA1 hashing + MysqlAdmin DB call here in real implementation
        SuccessMessage = "Password reset successful!";
        Username = string.Empty;
        NewPassword = string.Empty;
        ConfirmPassword = string.Empty;

    }

    private void HandleCancel()
    {
        CloseDialog?.Invoke();
    }
}
