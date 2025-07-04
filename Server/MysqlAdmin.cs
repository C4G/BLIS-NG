using BLIS_NG.Server;
using Microsoft.Extensions.Logging;

namespace BLIS_NG.server;

/// <summary>
/// Process wrapper class for running mysqladmin.exe.
/// Does not run like other processes since it will not continually operate.
/// </summary>
public class MySqlAdmin(ILoggerFactory loggerFactory) : BaseProcess(nameof(MySqlAdmin), loggerFactory)
{
  private static readonly string MysqlAdminPath = Path.Combine(
    ConfigurationFile.SERVER_BASE_DIR, "mysql", "bin", "mysqladmin.exe"
  );

  private readonly ILogger<MySqlAdmin> logger = loggerFactory.CreateLogger<MySqlAdmin>();
  private readonly string baseArguments = $"-u{MySqlIni.MYSQL_ROOT_USER} -p{MySqlIni.MYSQL_ROOT_PASSWORD} -h {MySqlIni.MYSQL_BIND_ADDRESS} --port {MySqlIni.MYSQL_PORT}";

  public override Task<ProcessResult> Run(CancellationToken cancellationToken = default)
  {
    throw new NotImplementedException();
  }

  public override void Stop()
  {
    throw new NotImplementedException();
  }

  public async Task Shutdown()
  {
    await Execute(MysqlAdminPath, $"{baseArguments} shutdown", (stdout) => logger.LogInformation("{Message}", stdout), (stderr) => logger.LogWarning("{Message}", stderr));
  }
}
