using System.Reactive;
using System.Security.Cryptography;
using System.Text;
using ReactiveUI;
using BLIS_NG.Server;

namespace BLIS_NG.ViewModels;

public class PasswordResetViewModel : ViewModelBase
{
    private readonly MySqlAdmin _mySqlAdmin;

    public ReactiveCommand<Unit, Unit> ProceedToVerifyCommand { get; }
    public ReactiveCommand<Unit, Unit> ConfirmResetCommand { get; }
    public ReactiveCommand<Unit, Unit> BackCommand { get; }
    public ReactiveCommand<Unit, Unit> CancelCommand { get; }

    // Step 1 — target user
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

    // Step 2 — supervisor credentials
    private string _supervisorUsername = string.Empty;
    public string SupervisorUsername
    {
        get => _supervisorUsername;
        set => this.RaiseAndSetIfChanged(ref _supervisorUsername, value);
    }

    private string _supervisorPassword = string.Empty;
    public string SupervisorPassword
    {
        get => _supervisorPassword;
        set => this.RaiseAndSetIfChanged(ref _supervisorPassword, value);
    }

    // Step visibility
    private int _currentStep = 1;
    public int CurrentStep
    {
        get => _currentStep;
        set
        {
            this.RaiseAndSetIfChanged(ref _currentStep, value);
            this.RaisePropertyChanged(nameof(IsStep1Visible));
            this.RaisePropertyChanged(nameof(IsStep2Visible));
        }
    }
    public bool IsStep1Visible => CurrentStep == 1;
    public bool IsStep2Visible => CurrentStep == 2;

    // Feedback
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

    public Action? CloseDialog { get; set; }

    public PasswordResetViewModel(MySqlAdmin mySqlAdmin)
    {
        _mySqlAdmin = mySqlAdmin;
        ProceedToVerifyCommand = ReactiveCommand.Create(HandleProceedToVerify);
        ConfirmResetCommand    = ReactiveCommand.CreateFromTask(HandleConfirmResetAsync);
        BackCommand            = ReactiveCommand.Create(HandleBack);
        CancelCommand          = ReactiveCommand.Create(HandleCancel);
    }

    private static string HashPasswordSha1(string password)
    {
        var salted = password + "This comment should suffice as salt.";
        var bytes  = SHA1.HashData(Encoding.UTF8.GetBytes(salted));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private void HandleProceedToVerify()
    {
        ErrorMessage   = string.Empty;
        SuccessMessage = string.Empty;

        if (string.IsNullOrWhiteSpace(Username))    { ErrorMessage = "Username is required.";     return; }
        if (string.IsNullOrWhiteSpace(NewPassword)) { ErrorMessage = "New password is required."; return; }
        if (NewPassword != ConfirmPassword)          { ErrorMessage = "Passwords do not match.";   return; }

        CurrentStep = 2;
    }

    private async Task HandleConfirmResetAsync()
    {
        ErrorMessage   = string.Empty;
        SuccessMessage = string.Empty;

        if (string.IsNullOrWhiteSpace(SupervisorUsername)) { ErrorMessage = "Supervisor username is required."; return; }
        if (string.IsNullOrWhiteSpace(SupervisorPassword)) { ErrorMessage = "Supervisor password is required."; return; }

        var supervisorHash = HashPasswordSha1(SupervisorPassword);
        var verified = await _mySqlAdmin.VerifyHigherRankedUser(SupervisorUsername, supervisorHash);

        if (!verified)
        {
            ErrorMessage = "Supervisor verification failed. Invalid credentials or insufficient rank.";
            return;
        }

        var sha1Hash = HashPasswordSha1(NewPassword);
        var success  = await _mySqlAdmin.ResetUserPassword(Username, sha1Hash);

        if (success)
            SuccessMessage = $"Password for '{Username}' was reset successfully.";
        else
            ErrorMessage = "Failed to reset password. Please check the username and try again.";
    }

    private void HandleBack()
    {
        ErrorMessage = string.Empty;
        CurrentStep  = 1;
    }

    private void HandleCancel() => CloseDialog?.Invoke();

    public void ResetForm()
    {
        Username           = string.Empty;
        NewPassword        = string.Empty;
        ConfirmPassword    = string.Empty;
        SupervisorUsername = string.Empty;
        SupervisorPassword = string.Empty;
        ErrorMessage       = string.Empty;
        SuccessMessage     = string.Empty;
        CurrentStep        = 1;
    }
}
