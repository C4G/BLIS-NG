// Much of this adopted from
// https://gist.github.com/AlexMAS/276eed492bc989e13dcce7c78b9e179d

using System.Diagnostics;
using BLIS_NG.Server;

namespace BLIS_NG.server;

public class MySqlServer : BaseProcess
{
  private readonly string MysqldPath = Path.Combine(
    ConfigurationFile.SERVER_BASE_DIR, "mysql", "bin", "mysqld.exe");

  private readonly string MysqlAdminPath = Path.Combine(
    ConfigurationFile.SERVER_BASE_DIR, "mysql", "bin", "mysqladmin.exe"
  );

  private readonly string ConfigPath = Path.Combine(
    ConfigurationFile.RUN_DIR, "my.ini"
  );

  private readonly string DataDir = Path.Combine(
    Directory.GetCurrentDirectory(),
    "dbdir"
  );

  private readonly MySqlIni mySqlIni = new();

  private readonly Process process;

  public bool IsRunning { get; private set; }

  public MySqlServer()
  {
    process = new Process();
    process.StartInfo.FileName = MysqldPath;
    process.StartInfo.Arguments = $"--defaults-file=\"{ConfigPath}\" --console --datadir=\"{DataDir}\"";

    Debug.WriteLine($"Running: {MysqldPath} {process.StartInfo.Arguments}");

    process.StartInfo.WindowStyle = ProcessWindowStyle.Normal;
    process.StartInfo.UseShellExecute = false;
    process.StartInfo.RedirectStandardOutput = true;
    process.StartInfo.RedirectStandardError = true;
  }

  public override async Task<ProcessResult> Run(Action<string>? stdOutConsumer = null, Action<string>? stdErrConsumer = null, CancellationToken cancellationToken = default)
  {
    mySqlIni.Write();

    var outputCloseEvent = new TaskCompletionSource<bool>();
    process.OutputDataReceived += (s, e) =>
    {
      if (e.Data == null)
      {
        outputCloseEvent.SetResult(true);
      }
      else
      {
        stdOutConsumer?.Invoke(e.Data);
      }
    };

    var errorCloseEvent = new TaskCompletionSource<bool>();
    process.ErrorDataReceived += (s, e) =>
    {
      if (e.Data == null)
      {
        errorCloseEvent.SetResult(true);
      }
      else
      {
        stdErrConsumer?.Invoke(e.Data);
      }
    };

    var result = new ProcessResult()
    {
      ExitCode = -1
    };
    IsRunning = false;

    try
    {
      IsRunning = process.Start();
    }
    catch (Exception e)
    {
      Debug.WriteLine($"Error starting process: {e.Message}");
    }

    if (IsRunning)
    {
      process.BeginOutputReadLine();
      process.BeginErrorReadLine();

      var exitWaiter = Task.Run(process.WaitForExit, cancellationToken);
      var processTask = Task.WhenAll(exitWaiter, outputCloseEvent.Task, errorCloseEvent.Task);

      await processTask;

      IsRunning = false;

      result = new ProcessResult()
      {
        ExitCode = process.ExitCode
      };
    }

    return result;
  }

  public override void Stop()
  {
    process.Kill();
    process.WaitForExit();
    IsRunning = false;
  }
}
