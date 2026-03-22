using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using ReactiveUI;
using Avalonia.Media;
using BLIS_NG.Config;
using BLIS_NG.Server;
using Microsoft.Extensions.Logging;

namespace BLIS_NG.ViewModels;

public class UpdateProgressViewModel : ViewModelBase
{
    private const string ExeName = "BLIS-NG.exe";
    private const string OldExeName = "BLIS-NG.exe.old";
    private const string StagingDir = "staging";

    private readonly ILogger<UpdateProgressViewModel> _logger;
    private readonly IMainServer _mainServer;

    private string _currentStageText = "Initializing...";
    private string _progressText = "0/4 Stages";
    private string _statusMessage = "Update In Progress";
    private int _percent = 0;
    private IBrush _statusColor = Brushes.Black;

    public string CurrentStageText { get => _currentStageText; set => this.RaiseAndSetIfChanged(ref _currentStageText, value); }
    public string ProgressText { get => _progressText; set => this.RaiseAndSetIfChanged(ref _progressText, value); }
    public string StatusMessage { get => _statusMessage; set => this.RaiseAndSetIfChanged(ref _statusMessage, value); }
    public int Percent { get => _percent; set => this.RaiseAndSetIfChanged(ref _percent, value); }
    public IBrush StatusColor { get => _statusColor; set => this.RaiseAndSetIfChanged(ref _statusColor, value); }

    public UpdateProgressViewModel(ILogger<UpdateProgressViewModel> logger, IMainServer mainServer)
    {
        _logger = logger;
        _mainServer = mainServer;
    }

    public async Task StartUpdate(string zipPath, Action onComplete)
    {
        string baseDir = ConfigurationFile.ResolveBaseDirectory();

        try
        {
            // Stage 1: Data Backup
            UpdateStage(1, "Stage 1: Backing up data...");
            _logger.LogInformation("Starting update from ZIP: {ZipPath}", zipPath);
            CreateAutomatedDatabaseBackup(baseDir);
            await Task.Delay(1000); // Small buffer for UX

            // Stage 2: Unpack ZIP and replace BLIS-NG.exe
            UpdateStage(2, "Stage 2: Unpacking ZIP file...");
            string stagingPath = Path.Combine(baseDir, StagingDir);
            await Task.Run(() => UnpackZip(zipPath, stagingPath));

            UpdateStage(2, "Stage 2: Locating new executable...");
            string? newExePath = FindFileRecursive(stagingPath, ExeName);
            if (newExePath == null)
            {
                _logger.LogError("{ExeName} not found in ZIP contents at {StagingPath}.", ExeName, stagingPath);
                throw new FileNotFoundException($"{ExeName} was not found in the update package.");
            }
            _logger.LogInformation("Found new executable at: {NewExePath}", newExePath);

            // Stop servers before replacing the executable
            UpdateStage(2, "Stage 2: Stopping servers...");
            _logger.LogInformation("Stopping servers before executable replacement.");
            await _mainServer.Stop();
            _logger.LogInformation("Servers stopped.");

            // Rename running exe (Windows allows renaming a locked/running exe)
            string currentExePath = Path.Combine(baseDir, ExeName);
            string oldExePath = Path.Combine(baseDir, OldExeName);
            ReplaceExecutable(currentExePath, oldExePath, newExePath);

            // Launch the new executable and exit the current process
            UpdateStage(2, "Stage 2: Launching updated application...");
            LaunchNewExecutable(currentExePath, baseDir);

            Percent = 100;
            StatusMessage = "Update Successful — Restarting...";
            StatusColor = Brushes.Green;
            _logger.LogInformation("Update completed successfully. New process launched, exiting current process.");

            await Task.Delay(2000);
            onComplete();

            // Exit the current process so the new one takes over
            Environment.Exit(0);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Update failed.");
            StatusMessage = $"Update Failed: {ex.Message}";
            StatusColor = Brushes.Red;

            await Task.Delay(3000);
            onComplete();
        }
    }

    private void UnpackZip(string zipPath, string stagingPath)
    {
        // Clean up any previous staging directory
        if (Directory.Exists(stagingPath))
        {
            _logger.LogInformation("Removing previous staging directory at {StagingPath}.", stagingPath);
            Directory.Delete(stagingPath, recursive: true);
        }

        _logger.LogInformation("Extracting ZIP to {StagingPath}.", stagingPath);
        ZipFile.ExtractToDirectory(zipPath, stagingPath);
        _logger.LogInformation("ZIP extraction completed.");
    }

    private static string? FindFileRecursive(string directory, string fileName)
    {
        return Directory.EnumerateFiles(directory, fileName, SearchOption.AllDirectories).FirstOrDefault();
    }

    private void ReplaceExecutable(string currentExePath, string oldExePath, string newExePath)
    {
        // Remove any leftover .old file from a previous update
        if (File.Exists(oldExePath))
        {
            _logger.LogInformation("Removing leftover {OldExeName} from previous update.", OldExeName);
            File.Delete(oldExePath);
        }

        // Rename the currently running exe — Windows allows this even while the file is locked
        if (File.Exists(currentExePath))
        {
            _logger.LogInformation("Renaming running executable {Current} to {Old}.", currentExePath, oldExePath);
            File.Move(currentExePath, oldExePath);
        }

        // Copy the new executable into place
        _logger.LogInformation("Copying new executable from {Source} to {Destination}.", newExePath, currentExePath);
        File.Copy(newExePath, currentExePath, overwrite: false);
        _logger.LogInformation("Executable replacement completed.");
    }

    private void LaunchNewExecutable(string exePath, string baseDir)
    {
        _logger.LogInformation("Launching new executable: {ExePath} --WorkingDirectory {BaseDir}", exePath, baseDir);
        Process.Start(new ProcessStartInfo
        {
            FileName = exePath,
            Arguments = $"--WorkingDirectory \"{new Uri(baseDir).AbsoluteUri}\"",
            UseShellExecute = true,
        });
    }

    private void UpdateStage(int stage, string text)
    {
        CurrentStageText = text;
        ProgressText = $"{stage}/4 Stages";
        Percent = (int)((stage - 1) / 4.0 * 100);
    }

    private void CreateAutomatedDatabaseBackup(string baseDir)
    {
        string dbSource = Path.Combine(baseDir, "dbdir");
        string backupRoot = Path.Combine(baseDir, "backups");
        string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string fullDestination = Path.Combine(backupRoot, $"DB_Backup_{timestamp}");

        if (Directory.Exists(dbSource))
        {
            _logger.LogInformation("Creating automated backup at: {Path}", fullDestination);
            Directory.CreateDirectory(backupRoot);
            CopyDirectoryRecursive(dbSource, fullDestination);
            _logger.LogInformation("Automated database backup completed.");
        }
        else
        {
            _logger.LogWarning("Database source not found at {Path}. Backup skipped.", dbSource);
        }
    }

    private void CopyDirectoryRecursive(string source, string target)
    {
        Directory.CreateDirectory(target);
        foreach (string file in Directory.GetFiles(source))
            File.Copy(file, Path.Combine(target, Path.GetFileName(file)), true);
        foreach (string dir in Directory.GetDirectories(source))
            CopyDirectoryRecursive(dir, Path.Combine(target, Path.GetFileName(dir)));
    }

    /// <summary>
    /// Cleans up leftover .old executable from a previous update.
    /// Should be called early during application startup.
    /// </summary>
    public static void CleanupOldExecutable()
    {
        try
        {
            string baseDir = ConfigurationFile.ResolveBaseDirectory();
            string oldExePath = Path.Combine(baseDir, OldExeName);
            if (File.Exists(oldExePath))
            {
                File.Delete(oldExePath);
                // Using Serilog static logger since this runs before DI is set up
                Serilog.Log.Information("Cleaned up old executable: {Path}", oldExePath);
            }
        }
        catch (Exception ex)
        {
            Serilog.Log.Warning(ex, "Failed to clean up old executable. Will retry on next startup.");
        }
    }
}
