using BLIS_NG.Server;
using Microsoft.Extensions.Logging;

namespace BLIS_NG.server;

public class MySqlServer(ILoggerFactory loggerFactory) : BaseProcess(nameof(MySqlServer), loggerFactory)
{
  private static readonly string MysqldPath = Path.Combine(
    ConfigurationFile.SERVER_BASE_DIR, "mysql", "bin", "mysqld.exe");

  private static readonly string ConfigPath = Path.Combine(
    ConfigurationFile.RUN_DIR, "my.ini"
  );

  private static readonly string DataDir = Path.Combine(
    Directory.GetCurrentDirectory(),
    "dbdir"
  );

  private static readonly string Arguments = $"--defaults-file=\"{ConfigPath}\" --console --datadir=\"{DataDir}\"";

  private readonly MySqlIni mySqlIni = new();
  private readonly MySqlAdmin mySqlAdmin = new(loggerFactory);

  public override async Task<ProcessResult> Run(Action<string>? stdOutConsumer = null, Action<string>? stdErrConsumer = null, CancellationToken cancellationToken = default)
  {
    mySqlIni.Write();
    return await Execute(MysqldPath, Arguments, null, stdOutConsumer, stdErrConsumer, cancellationToken);
  }

  public override async void Stop()
  {
    await mySqlAdmin.Shutdown();
  }
}
