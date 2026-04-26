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
    private string _statusMessage = "Update In Progress";
    private int _percent = 0;
    private IBrush _statusColor = Brushes.Black;

    public string CurrentStageText { get => _currentStageText; set => this.RaiseAndSetIfChanged(ref _currentStageText, value); }
    public string StatusMessage { get => _statusMessage; set => this.RaiseAndSetIfChanged(ref _statusMessage, value); }
    public int Percent { get => _percent; set => this.RaiseAndSetIfChanged(ref _percent, value); }
    public IBrush StatusColor { get => _statusColor; set => this.RaiseAndSetIfChanged(ref _statusColor, value); }

    private const int TotalStages = 10;

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
            // Stage 1: Unpack ZIP
            UpdateStage(1, "Unpacking ZIP file...");
            _logger.LogInformation("Starting update from ZIP: {ZipPath}", zipPath);
            string stagingPath = Path.Join(baseDir, StagingDir);
            string effectiveStagingPath = "";
            await Task.Run(() => effectiveStagingPath = UnpackZip(zipPath, stagingPath));

            // Stage 2: Validate contents before touching servers or data
            UpdateStage(2, "Validating update package...");
            var versionFile = VersionFile.Load(effectiveStagingPath);
            string? newExeInZip = FindFileRecursive(effectiveStagingPath, ExeName);
            string stagingServerPath = Path.Join(effectiveStagingPath, ServerDir);

            if (versionFile == null || string.IsNullOrWhiteSpace(versionFile.Version)
                || newExeInZip == null
                || !Directory.Exists(stagingServerPath))
            {
                _logger.LogError("Update ZIP is missing required files. version.json={HasVersion}, {ExeName}={HasExe}, server/={HasServer}",
                    versionFile?.Version != null, ExeName, newExeInZip != null, Directory.Exists(stagingServerPath));
                ShowError("Update Failed: Incompatible Update ZIP File");
                return;
            }

            string newVersion = versionFile.Version;
            _logger.LogInformation("Update package version: {NewVersion}", newVersion);

            // Read current state and check for same-version update
            var state = StateFile.Load(baseDir);
            string currentVersion = state.ActiveVersion;
            _logger.LogInformation("Current active version: {CurrentVersion}", currentVersion);

            if (string.Equals(currentVersion, newVersion, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("Update version {NewVersion} matches current active version. Aborting.", newVersion);
                ShowError("Update Failed: Update Version is Same as Current Version");
                return;
            }

            // Stage 3: Database backup
            UpdateStage(3, "Backing up database...");
            CreateAutomatedDatabaseBackup(baseDir);

            // Stage 4: Stop servers
            UpdateStage(4, "Stopping servers...");
            _logger.LogInformation("Stopping servers before update.");
            await _mainServer.Stop();
            _logger.LogInformation("Servers stopped.");

            // Stage 5: Backup current server
            UpdateStage(5, "Backing up current server...");
            string serverPath = Path.Join(baseDir, ServerDir);
            if (Directory.Exists(serverPath))
            {
                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string serverBackupPath = Path.Join(baseDir, BackupsDir, $"server-{currentVersion}-{timestamp}");
                _logger.LogInformation("Moving current server to backup: {BackupPath}", serverBackupPath);
                Directory.CreateDirectory(Path.Join(baseDir, BackupsDir));
                Directory.Move(serverPath, serverBackupPath);
                _logger.LogInformation("Server backup completed.");
            }
            else
            {
                _logger.LogWarning("No existing server directory found at {ServerPath}. Skipping server backup.", serverPath);
            }

            // Stage 6: Install new server
            UpdateStage(6, "Installing new server...");
            _logger.LogInformation("Copying new server from {Source} to {Destination}.", stagingServerPath, serverPath);
            CopyDirectoryRecursive(stagingServerPath, serverPath);
            _logger.LogInformation("New server installed.");

            // Stage 7: Install release files
            UpdateStage(7, "Installing release files...");
            string releasePath = Path.Join(baseDir, ReleasesDir, newVersion);
            _logger.LogInformation("Copying release files to {ReleasePath}.", releasePath);
            Directory.CreateDirectory(releasePath);

            foreach (var dir in Directory.GetDirectories(effectiveStagingPath))
            {
                string dirName = Path.GetFileName(dir);
                if (string.Equals(dirName, ServerDir, StringComparison.OrdinalIgnoreCase))
                    continue;
                CopyDirectoryRecursive(dir, Path.Join(releasePath, dirName));
            }
            foreach (var file in Directory.GetFiles(effectiveStagingPath))
            {
                string fileName = Path.GetFileName(file);
                if (string.Equals(fileName, ExeName, StringComparison.OrdinalIgnoreCase))
                    continue;
                File.Copy(file, Path.Join(releasePath, fileName), overwrite: true);
            }
            _logger.LogInformation("Release files installed.");

            // Stage 8: Update configuration
            UpdateStage(8, "Updating configuration...");
            state.PreviousVersion = currentVersion;
            state.ActiveVersion = newVersion;
            state.Save(baseDir);
            _logger.LogInformation("state.json updated: active_version={NewVersion}, previous_version={CurrentVersion}", newVersion, currentVersion);

            // Stage 9: Replace executable
            UpdateStage(9, "Replacing executable...");
            string currentExePath = Path.Join(baseDir, ExeName);
            string oldExePath = Path.Join(baseDir, OldExeName);
            ReplaceExecutable(currentExePath, oldExePath, newExeInZip);

            // Stage 10: Launch new application
            UpdateStage(10, "Launching updated application...");
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
            ShowError($"Update Failed: {ex.Message}");
        }
    }

    private void ShowError(string message)
    {
        CurrentStageText = "";
        StatusMessage = message;
        StatusColor = Brushes.Red;
        Percent = 0;
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
        CurrentStageText = $"Step {stage}/{TotalStages}: {text}";
        Percent = (int)((stage - 1) / (double)TotalStages * 100);
    }

    private void CreateAutomatedDatabaseBackup(string baseDir)
    {
        string dbSource = Path.Join(baseDir, "dbdir");
        string backupRoot = Path.Join(baseDir, BackupsDir);
        string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string fullDestination = Path.Join(backupRoot, $"DB_Backup_{timestamp}");

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
            File.Copy(file, Path.Join(target, Path.GetFileName(file)), true);
        foreach (string dir in Directory.GetDirectories(source))
            CopyDirectoryRecursive(dir, Path.Join(target, Path.GetFileName(dir)));
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

            string oldExePath = Path.Join(baseDir, OldExeName);
            if (File.Exists(oldExePath))
            {
                File.Delete(oldExePath);
                Serilog.Log.Information("Cleaned up old executable: {Path}", oldExePath);
            }

            string stagingPath = Path.Join(baseDir, StagingDir);
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
