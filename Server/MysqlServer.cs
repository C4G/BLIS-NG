using BLIS_NG.Config;
using Microsoft.Extensions.Logging;

namespace BLIS_NG.Server;

public class MySqlServer(ILogger<MySqlServer> logger, MySqlIni mySqlIni, MySqlAdmin mySqlAdmin) : BaseProcess(nameof(MySqlServer), logger)
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

  private readonly ILogger<MySqlServer> logger = logger;
  private readonly MySqlIni mySqlIni = mySqlIni;
  private readonly MySqlAdmin mySqlAdmin = mySqlAdmin;

  public async Task<ProcessResult> Run(CancellationToken cancellationToken = default)
  {
    mySqlIni.Write();
    return await Execute(MysqldPath, Arguments, null, (stdout) => logger.LogInformation("{}", stdout), (stderr) => logger.LogWarning("{}", stderr), cancellationToken);
  }

  public override async void Stop()
  {
    await mySqlAdmin.Shutdown();
  }
}
