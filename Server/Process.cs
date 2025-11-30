using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace BLIS_NG.Server;

public record ProcessResult(int ExitCode)
{
    public readonly int ExitCode = ExitCode;
}

// The initial code for this class was adapted from:
// https://gist.github.com/AlexMAS/276eed492bc989e13dcce7c78b9e179d

public abstract class BaseProcess(string ProcessName, ILogger logger, bool singleton = true) : IDisposable
{
    private const int FAILED_TO_LAUNCH = -1;

    private readonly ILogger logger = logger;
    private readonly string ProcessName = ProcessName;
    private readonly bool singleton = singleton;
    private Process? process;

    public bool IsRunning { get => process != null; }

    protected async Task<ProcessResult> Execute(string exePath, string arguments, IDictionary<string, string>? environment = null, Action<string>? stdOutConsumer = null, Action<string>? stdErrConsumer = null, CancellationToken cancellationToken = default)
    {
        var result = new ProcessResult(FAILED_TO_LAUNCH);

        if (singleton && IsRunning)
        {
            logger.LogWarning("Attempted to start {ProcessName} when it is already running.", ProcessName);
            return result;
        }

        // If you really want to know...
        // logger.LogInformation("{ExePath} {Arguments}", exePath, arguments);

        process = new Process()
        {
            StartInfo = new ProcessStartInfo()
            {
                // Set by internal usage
                FileName = exePath,
                Arguments = arguments,

                // Things we want set for all processes
                WindowStyle = ProcessWindowStyle.Hidden,
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

            try {
                result = new ProcessResult(ExitCode: process?.ExitCode != null ? process.ExitCode : 1);
            } catch (InvalidOperationException e)
            {
                logger.LogCritical(e, "Process did not complete.");
                result = new ProcessResult(ExitCode: 1);
            }
        }

        process = null;

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
