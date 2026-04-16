using System;
using System.IO;
using System.Threading.Tasks;
using ReactiveUI;
using Avalonia.Media;
using BLIS_NG.Config;

namespace BLIS_NG.ViewModels;

public class UpdateProgressViewModel : ViewModelBase
{
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

    public async Task StartUpdate(string zipPath, Action onComplete)
    {
        try
        {
            // Stage 1: Data Backup
            UpdateStage(1, "Stage 1: Backing up data...");
            CreateAutomatedDatabaseBackup();
            await Task.Delay(1000); // Small buffer for UX

            // Stage 2: Unpacking Zip (Simulation)
            UpdateStage(2, "Stage 2: Unpacking ZIP file...");
            await Task.Delay(5000);

            // Stage 3: Installing Package (Simulation)
            UpdateStage(3, "Stage 3: Installing new package...");
            await Task.Delay(5000);

            // Stage 4: Migrating Data (Simulation)
            UpdateStage(4, "Stage 4: Migrating data...");
            await Task.Delay(5000);

            Percent = 100;
            StatusMessage = "Update Successful";
            StatusColor = Brushes.Green;
        }
        catch (Exception)
        {
            StatusMessage = "Update Failed";
            StatusColor = Brushes.Red;
        }

        await Task.Delay(3000); // Wait 3 seconds before closing
        onComplete();
    }

    private void UpdateStage(int stage, string text)
    {
        CurrentStageText = text;
        ProgressText = $"{stage}/4 Stages";
        Percent = (int)((stage - 1) / 4.0 * 100);
    }

    private void CreateAutomatedDatabaseBackup()
    {
        string baseDir = ConfigurationFile.ResolveBaseDirectory();
        string dbSource = Path.Combine(baseDir, "dbdir");
        string backupRoot = Path.Combine(baseDir, "backups");
        string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string fullDestination = Path.Combine(backupRoot, $"DB_Backup_{timestamp}");

        if (Directory.Exists(dbSource))
        {
            Directory.CreateDirectory(backupRoot);
            CopyDirectoryRecursive(dbSource, fullDestination);
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
}
