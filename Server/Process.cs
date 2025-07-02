namespace BLIS_NG.Server;

public record ProcessResult
{
  public int ExitCode { get; init; }
}

public interface IExternalProcess : IDisposable
{
  public Task<ProcessResult> Run(Action<string>? stdOutConsumer = null, Action<string>? stdErrConsumer = null, CancellationToken cancellationToken = default);
  public void Stop();
}

public abstract class BaseProcess : IExternalProcess
{
  public abstract Task<ProcessResult> Run(Action<string>? stdOutConsumer = null, Action<string>? stdErrConsumer = null, CancellationToken cancellationToken = default);

  public abstract void Stop();

  public void Dispose()
  {
    Stop();
    GC.SuppressFinalize(this);
  }
}
