using BLIS_NG.Config;
using Microsoft.Extensions.Logging;

namespace BLIS_NG.Server;

/// <summary>
/// Process wrapper class for running mysql_upgrade.exe.
/// Does not run like other processes since it will not continually operate.
/// </summary>
public class MySqlUpgrade(ILogger<MySqlUpgrade> logger) : BaseProcess(nameof(MySqlUpgrade), logger, singleton: false)
{
  public static readonly string MysqlUpgradePath = Path.Combine(
    ConfigurationFile.SERVER_BASE_DIR, "mysql", "bin", "mysql_upgrade.exe");

  private readonly ILogger<MySqlUpgrade> logger = logger;
  private readonly string Arguments = $"-h {MySqlIni.MYSQL_BIND_ADDRESS} --port {MySqlIni.MYSQL_PORT}";

  public override void Stop()
  {
    // No-op since this process is not long-running.
    return;
  }

  public async Task Run()
  {
    await Execute(MysqlUpgradePath, Arguments, null, (stdout) => logger.LogInformation("{Message}", stdout), (stderr) => logger.LogWarning("{Message}", stderr));
  }
}
