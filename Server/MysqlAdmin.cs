using BLIS_NG.Config;
using Microsoft.Extensions.Logging;

namespace BLIS_NG.Server;

/// <summary>
/// Process wrapper class for running mysqladmin.exe.
/// Does not run like other processes since it will not continually operate.
/// </summary>
public class MySqlAdmin(ILogger<MySqlAdmin> logger) : BaseProcess(nameof(MySqlAdmin), logger)
{
  private static readonly string MysqlAdminPath = Path.Combine(
    ConfigurationFile.SERVER_BASE_DIR, "mysql", "bin", "mysqladmin.exe"
  );

  private readonly ILogger<MySqlAdmin> logger = logger;
  private readonly string baseArguments = $"-u{MySqlIni.MYSQL_ROOT_USER} -p{MySqlIni.MYSQL_ROOT_PASSWORD} -h {MySqlIni.MYSQL_BIND_ADDRESS} --port {MySqlIni.MYSQL_PORT}";

  public override void Stop()
  {
    // No-op since this process is not long-running.
    return;
  }

  public async Task<bool> Ping()
  {
    // Not logging stdout here since it will just fill up logs.
    var result = await Execute(MysqlAdminPath, $"{baseArguments} ping", null, null, (stderr) => logger.LogWarning("{Message}", stderr));
    return result.ExitCode == 0;
  }

  public async Task Shutdown()
  {
    await Execute(MysqlAdminPath, $"{baseArguments} shutdown", null, (stdout) => logger.LogInformation("{Message}", stdout), (stderr) => logger.LogWarning("{Message}", stderr));
  }
}
