// The initial code for this class was adapted from
// https://gist.github.com/AlexMAS/276eed492bc989e13dcce7c78b9e179d

using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace BLIS_NG.Server;

public record ProcessResult(int ExitCode)
{
  public readonly int ExitCode = ExitCode;
}

public interface IExternalProcess : IDisposable
{
  public Task<ProcessResult> Run(Action<string>? stdOutConsumer = null, Action<string>? stdErrConsumer = null, CancellationToken cancellationToken = default);
  public void Stop();
}

public abstract class BaseProcess(string ProcessName, ILoggerFactory loggerFactory) : IExternalProcess
{
  public const int FAILED_TO_LAUNCH = -1;

  private readonly ILogger<BaseProcess> logger = loggerFactory.CreateLogger<BaseProcess>();
  protected readonly string ProcessName = ProcessName;
  private Process? process;
  public bool IsRunning { get => process != null; }

  public abstract Task<ProcessResult> Run(Action<string>? stdOutConsumer = null, Action<string>? stdErrConsumer = null, CancellationToken cancellationToken = default);

  internal async Task<ProcessResult> Execute(string exePath, string arguments, IDictionary<string, string>? environment = null, Action<string>? stdOutConsumer = null, Action<string>? stdErrConsumer = null, CancellationToken cancellationToken = default)
  {
    var result = new ProcessResult(FAILED_TO_LAUNCH);

    if (IsRunning)
    {
      logger.LogWarning("Attempted to start {ProcessName} when it is already running.", ProcessName);
      return result;
    }

    logger.LogInformation("{ExePath} {Arguments}", exePath, arguments);

    process = new Process()
    {
      StartInfo = new ProcessStartInfo()
      {
        // Set by internal usage
        FileName = exePath,
        Arguments = arguments,

        // Things we want set for all processes
        WindowStyle = ProcessWindowStyle.Normal,
        UseShellExecute = false,
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        RedirectStandardInput = true,
      }
    };

    if (environment != null)
    {
      foreach (var (key, value) in environment)
      {
        process.StartInfo.EnvironmentVariables[key] = value;
      }
    }

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

    bool started = false;
    try
    {
      started = process.Start();
    }
    catch (Exception e)
    {
      logger.LogError(e, "Process {ProcessName} failed to start.", ProcessName);
      process = null;
      return result;
    }

    if (started)
    {
      process.BeginOutputReadLine();
      process.BeginErrorReadLine();

      var exitWaiter = Task.Run(process.WaitForExit, cancellationToken);
      await Task.WhenAll(exitWaiter, outputCloseEvent.Task, errorCloseEvent.Task);

      result = new ProcessResult(ExitCode: process.ExitCode);
      process = null;
    }

    return result;
  }
  public abstract void Stop();

  protected void Kill()
  {
    process?.Kill();
  }

  public void Dispose()
  {
    Stop();
    GC.SuppressFinalize(this);
  }
}
