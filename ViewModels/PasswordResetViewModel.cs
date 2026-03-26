using System.Reactive;
using System.Security.Cryptography;
using System.Text;
using ReactiveUI;
using BLIS_NG.Server;

namespace BLIS_NG.ViewModels;

public class PasswordResetViewModel : ViewModelBase
{
    private readonly MySqlAdmin _mySqlAdmin;

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

    // Callback so the dialog can close itself (set from the View's code-behind)
    public Action? CloseDialog { get; set; }

    public PasswordResetViewModel(MySqlAdmin mySqlAdmin)
    {
        _mySqlAdmin = mySqlAdmin;
        ResetPasswordCommand = ReactiveCommand.CreateFromTask(HandleResetAsync);
        CancelCommand = ReactiveCommand.Create(HandleCancel);
    }

    private static string HashPasswordSha1(string password)
    {
        var salted = password + "This comment should suffice as salt.";
        var bytes = SHA1.HashData(Encoding.UTF8.GetBytes(salted));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private async Task HandleResetAsync()
    {

        ErrorMessage = string.Empty;
        SuccessMessage = string.Empty;

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

        var sha1Hash = HashPasswordSha1(NewPassword);

        var success = await _mySqlAdmin.ResetUserPassword(Username, sha1Hash);

        if (success)
        {
            SuccessMessage = "Password reset successful!";
            Username = string.Empty;
            NewPassword = string.Empty;
            ConfirmPassword = string.Empty;
        }
        else
        {
            ErrorMessage = "Failed to reset password. Please check the username and try again.";
        }
    }

    private void HandleCancel()
    {
        CloseDialog?.Invoke();
    }
}