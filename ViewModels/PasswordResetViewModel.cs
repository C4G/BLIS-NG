using System.Reactive;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using ReactiveUI;
using BLIS_NG.Server;

namespace BLIS_NG.ViewModels;

public class PasswordResetViewModel : ViewModelBase
{
    // role hierarchy dictionary
    private static readonly Dictionary<int, int> RolePower = new()
    {
        { 0, 1 },  // TECH_RW
        { 1, 1 },  // TECH_RO
        { 5, 1 },  // CLERK
        { 2, 2 },  // ADMIN
        { 4, 3 },  // COUNTRYDIR
        { 3, 4 },  // SUPERADMIN
    };

    private readonly MySql _mySql;

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
        set
        {
            this.RaiseAndSetIfChanged(ref _newPassword, value);
            this.RaisePropertyChanged(nameof(PasswordStrength));
            this.RaisePropertyChanged(nameof(PasswordStrengthLabel));
            this.RaisePropertyChanged(nameof(PasswordStrengthColor));
            this.RaisePropertyChanged(nameof(PasswordStrengthWidth));
            this.RaisePropertyChanged(nameof(PasswordRequirementsMet));
        }
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

    // ── Password strength ──────────────────────────────────────────────────

    // Returns 0–4 based on how many criteria are met
    public int PasswordStrength
    {
        get
        {
            if (string.IsNullOrEmpty(NewPassword)) return 0;
            int score = 0;
            if (NewPassword.Length >= 20) score++;
            if (Regex.IsMatch(NewPassword, "[a-z]")) score++;
            if (Regex.IsMatch(NewPassword, "[A-Z]")) score++;
            if (Regex.IsMatch(NewPassword, "[0-9]")) score++;
            if (Regex.IsMatch(NewPassword, "[^a-zA-Z0-9]")) score++;
            return score;
        }
    }

    public string PasswordStrengthLabel => PasswordStrength switch
    {
        0 => "",
        1 => "Very Weak",
        2 => "Weak",
        3 => "Fair",
        4 => "Strong",
        5 => "Very Strong",
        _ => ""
    };

    public string PasswordStrengthColor => PasswordStrength switch
    {
        1 => "#D32F2F",
        2 => "#F57C00",
        3 => "#FBC02D",
        4 => "#388E3C",
        5 => "#1B5E20",
        _ => "Transparent"
    };

    // Width as a fraction of 300px bar (60px per point)
    public double PasswordStrengthWidth => PasswordStrength * 60.0;

    // True when minimum NIST-aligned requirements are met (length + 2 classes)
    public bool PasswordRequirementsMet
    {
        get
        {
            if (NewPassword.Length < 20) return false;
            int classes = 0;
            if (Regex.IsMatch(NewPassword, "[a-z]")) classes++;
            if (Regex.IsMatch(NewPassword, "[A-Z]")) classes++;
            if (Regex.IsMatch(NewPassword, "[0-9]")) classes++;
            if (Regex.IsMatch(NewPassword, "[^a-zA-Z0-9]")) classes++;
            return classes >= 2;
        }
    }

    // ── Feedback ───────────────────────────────────────────────────────────

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

    // ── Constructor ────────────────────────────────────────────────────────

    public PasswordResetViewModel(MySql mySql)
    {
        _mySql = mySql;
        ProceedToVerifyCommand = ReactiveCommand.Create(HandleProceedToVerify);
        ConfirmResetCommand = ReactiveCommand.CreateFromTask(HandleConfirmResetAsync);
        BackCommand = ReactiveCommand.Create(HandleBack);
        CancelCommand = ReactiveCommand.Create(HandleCancel);
    }

    // ── Helpers ────────────────────────────────────────────────────────────

    private static string HashPasswordSha1(string password)
    {
        var salted = password + "This comment should suffice as salt.";
        var bytes = SHA1.HashData(Encoding.UTF8.GetBytes(salted));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private string? ValidatePassword(string password)
    {
        if (password.Length < 20)
            return "Password must be at least 20 characters.";

        int classes = 0;
        if (Regex.IsMatch(password, "[a-z]")) classes++;
        if (Regex.IsMatch(password, "[A-Z]")) classes++;
        if (Regex.IsMatch(password, "[0-9]")) classes++;
        if (Regex.IsMatch(password, "[^a-zA-Z0-9]")) classes++;

        if (classes < 2)
            return "Password must include at least 2 character types (uppercase, lowercase, numbers, symbols).";

        return null; // valid
    }

    // ── Handlers ───────────────────────────────────────────────────────────

    private void HandleProceedToVerify()
    {
        ErrorMessage = string.Empty;
        SuccessMessage = string.Empty;

        if (string.IsNullOrWhiteSpace(Username))
        { ErrorMessage = "Username is required."; return; }

        if (string.IsNullOrWhiteSpace(NewPassword))
        { ErrorMessage = "New password is required."; return; }

        var passwordError = ValidatePassword(NewPassword);
        if (passwordError is not null)
        { ErrorMessage = passwordError; return; }

        if (NewPassword != ConfirmPassword)
        { ErrorMessage = "Passwords do not match."; return; }

        CurrentStep = 2;
    }

    private async Task HandleConfirmResetAsync()
    {
        ErrorMessage = string.Empty;
        SuccessMessage = string.Empty;

        if (string.IsNullOrWhiteSpace(SupervisorUsername))
        { ErrorMessage = "Supervisor username is required."; return; }
        if (string.IsNullOrWhiteSpace(SupervisorPassword))
        { ErrorMessage = "Supervisor password is required."; return; }

        var supervisorHash = HashPasswordSha1(SupervisorPassword);
        var supervisorLevel = await _mySql.GetVerifiedUserLevel(SupervisorUsername, supervisorHash);

        if (supervisorLevel is null)
        {
            ErrorMessage = "Supervisor verification failed. Invalid credentials.";
            return;
        }

        var targetLevel = await _mySql.GetUserLevel(Username);

        if (targetLevel is null)
        {
            ErrorMessage = $"Could not find user '{Username}'.";
            return;
        }

        var supervisorPower = RolePower.GetValueOrDefault(supervisorLevel.Value, 0);
        var targetPower = RolePower.GetValueOrDefault(targetLevel.Value, 0);

        if (supervisorPower <= targetPower)
        {
            ErrorMessage = "Supervisor does not have sufficient rank to reset this user's password.";
            return;
        }

        var sha1Hash = HashPasswordSha1(NewPassword);
        var success = await _mySql.ResetUserPassword(Username, sha1Hash);

        if (success)
            SuccessMessage = $"Password for '{Username}' was reset successfully.";
        else
            ErrorMessage = "Failed to reset password. Please check the username and try again.";
    }

    private void HandleBack()
    {
        ErrorMessage = string.Empty;
        CurrentStep = 1;
    }

    private void HandleCancel() => ResetForm();

    public void ResetForm()
    {
        Username = string.Empty;
        NewPassword = string.Empty;
        ConfirmPassword = string.Empty;
        SupervisorUsername = string.Empty;
        SupervisorPassword = string.Empty;
        ErrorMessage = string.Empty;
        SuccessMessage = string.Empty;
        CurrentStep = 1;
    }
}
