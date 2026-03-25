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
    private const string ServerDir = "server";
    private const string ReleasesDir = "releases";
    private const string BackupsDir = "backups";

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
            await Task.Delay(1000);

            // Stage 2: Unpack, copy files, replace exe
            UpdateStage(2, "Stage 2: Unpacking ZIP file...");
            string stagingPath = Path.Combine(baseDir, StagingDir);
            string effectiveStagingPath = "";
            await Task.Run(() => effectiveStagingPath = UnpackZip(zipPath, stagingPath));

            // Read version.json from staging to get NEW_VERSION
            var versionFile = VersionFile.Load(effectiveStagingPath);
            if (versionFile == null || string.IsNullOrWhiteSpace(versionFile.Version))
            {
                _logger.LogError("version.json not found or missing version field in {StagingPath}.", effectiveStagingPath);
                throw new FileNotFoundException("version.json was not found or is invalid in the update package.");
            }
            string newVersion = versionFile.Version;
            _logger.LogInformation("Update package version: {NewVersion}", newVersion);

            // Read current state
            var state = StateFile.Load(baseDir);
            string currentVersion = state.ActiveVersion;
            _logger.LogInformation("Current active version: {CurrentVersion}", currentVersion);

            // Backup current server folder
            UpdateStage(2, "Stage 2: Backing up current server...");
            string serverPath = Path.Combine(baseDir, ServerDir);
            if (Directory.Exists(serverPath))
            {
                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string serverBackupPath = Path.Combine(baseDir, BackupsDir, $"server-{currentVersion}-{timestamp}");
                _logger.LogInformation("Moving current server to backup: {BackupPath}", serverBackupPath);
                Directory.CreateDirectory(Path.Combine(baseDir, BackupsDir));
                Directory.Move(serverPath, serverBackupPath);
                _logger.LogInformation("Server backup completed.");
            }
            else
            {
                _logger.LogWarning("No existing server directory found at {ServerPath}. Skipping server backup.", serverPath);
            }

            // Copy new server folder from staging
            UpdateStage(2, "Stage 2: Installing new server...");
            string stagingServerPath = Path.Combine(effectiveStagingPath, ServerDir);
            if (Directory.Exists(stagingServerPath))
            {
                _logger.LogInformation("Copying new server from {Source} to {Destination}.", stagingServerPath, serverPath);
                CopyDirectoryRecursive(stagingServerPath, serverPath);
                _logger.LogInformation("New server installed.");
            }
            else
            {
                _logger.LogWarning("No server directory found in update package at {Path}.", stagingServerPath);
            }

            // Copy everything else (except BLIS-NG.exe and server/) into releases/NEW_VERSION/
            UpdateStage(2, "Stage 2: Installing release files...");
            string releasePath = Path.Combine(baseDir, ReleasesDir, newVersion);
            _logger.LogInformation("Copying release files to {ReleasePath}.", releasePath);
            Directory.CreateDirectory(releasePath);

            foreach (var dir in Directory.GetDirectories(effectiveStagingPath))
            {
                string dirName = Path.GetFileName(dir);
                if (string.Equals(dirName, ServerDir, StringComparison.OrdinalIgnoreCase))
                    continue;
                CopyDirectoryRecursive(dir, Path.Combine(releasePath, dirName));
            }
            foreach (var file in Directory.GetFiles(effectiveStagingPath))
            {
                string fileName = Path.GetFileName(file);
                if (string.Equals(fileName, ExeName, StringComparison.OrdinalIgnoreCase))
                    continue;
                File.Copy(file, Path.Combine(releasePath, fileName), overwrite: true);
            }
            _logger.LogInformation("Release files installed.");

            // Update state.json
            UpdateStage(2, "Stage 2: Updating state...");
            state.PreviousVersion = currentVersion;
            state.ActiveVersion = newVersion;
            state.Save(baseDir);
            _logger.LogInformation("state.json updated: active_version={NewVersion}, previous_version={CurrentVersion}", newVersion, currentVersion);

            // Stop servers before replacing the executable
            UpdateStage(2, "Stage 2: Stopping servers...");
            _logger.LogInformation("Stopping servers before executable replacement.");
            await _mainServer.Stop();
            _logger.LogInformation("Servers stopped.");

            // Replace BLIS-NG.exe
            UpdateStage(2, "Stage 2: Replacing executable...");
            string currentExePath = Path.Combine(baseDir, ExeName);
            string oldExePath = Path.Combine(baseDir, OldExeName);
            string? newExePath = FindFileRecursive(effectiveStagingPath, ExeName);
            if (newExePath == null)
            {
                _logger.LogError("{ExeName} not found in staging at {StagingPath}.", ExeName, effectiveStagingPath);
                throw new FileNotFoundException($"{ExeName} was not found in the update package.");
            }
            ReplaceExecutable(currentExePath, oldExePath, newExePath);

            // Launch the new executable and exit
            UpdateStage(2, "Stage 2: Launching updated application...");
            LaunchNewExecutable(currentExePath, baseDir);

            Percent = 100;
            StatusMessage = "Update Successful — Restarting...";
            StatusColor = Brushes.Green;
            _logger.LogInformation("Update completed successfully. New process launched, exiting current process.");

            await Task.Delay(2000);
            onComplete();

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

    /// <summary>
    /// Extracts the ZIP and returns the effective root path of the contents.
    /// If the ZIP contains a single root folder, returns that folder's path
    /// so the rest of the code doesn't need to know about the ZIP's internal structure.
    /// </summary>
    private string UnpackZip(string zipPath, string stagingPath)
    {
        if (Directory.Exists(stagingPath))
        {
            _logger.LogInformation("Removing previous staging directory at {StagingPath}.", stagingPath);
            Directory.Delete(stagingPath, recursive: true);
        }

        _logger.LogInformation("Extracting ZIP to {StagingPath}.", stagingPath);
        ZipFile.ExtractToDirectory(zipPath, stagingPath);
        _logger.LogInformation("ZIP extraction completed.");

        // If the ZIP had a single root folder, use that as the effective staging root
        var dirs = Directory.GetDirectories(stagingPath);
        var files = Directory.GetFiles(stagingPath);
        if (dirs.Length == 1 && files.Length == 0)
        {
            _logger.LogInformation("ZIP contained single root folder: {Folder}. Using it as staging root.", Path.GetFileName(dirs[0]));
            return dirs[0];
        }

        return stagingPath;
    }

    private static string? FindFileRecursive(string directory, string fileName)
    {
        return Directory.EnumerateFiles(directory, fileName, SearchOption.AllDirectories).FirstOrDefault();
    }

    private void ReplaceExecutable(string currentExePath, string oldExePath, string newExePath)
    {
        if (File.Exists(oldExePath))
        {
            _logger.LogInformation("Removing leftover {OldExeName} from previous update.", OldExeName);
            File.Delete(oldExePath);
        }

        if (File.Exists(currentExePath))
        {
            _logger.LogInformation("Renaming running executable {Current} to {Old}.", currentExePath, oldExePath);
            File.Move(currentExePath, oldExePath);
        }

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
        string backupRoot = Path.Combine(baseDir, BackupsDir);
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
    /// Cleans up leftover artifacts from a previous update on startup.
    /// Deletes BLIS-NG.exe.old and the staging directory.
    /// </summary>
    public static void StartupCleanup()
    {
        try
        {
            string baseDir = ConfigurationFile.ResolveBaseDirectory();

            string oldExePath = Path.Combine(baseDir, OldExeName);
            if (File.Exists(oldExePath))
            {
                File.Delete(oldExePath);
                Serilog.Log.Information("Cleaned up old executable: {Path}", oldExePath);
            }

            string stagingPath = Path.Combine(baseDir, StagingDir);
            if (Directory.Exists(stagingPath))
            {
                Directory.Delete(stagingPath, recursive: true);
                Serilog.Log.Information("Cleaned up staging directory: {Path}", stagingPath);
            }
        }
        catch (Exception ex)
        {
            Serilog.Log.Warning(ex, "Failed to clean up update artifacts. Will retry on next startup.");
        }
    }
}
