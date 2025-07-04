using BLIS_NG.Server;
using Microsoft.Extensions.Logging;

namespace BLIS_NG.server;

public class MySqlAdmin(ILoggerFactory loggerFactory) : BaseProcess(nameof(MySqlAdmin), loggerFactory)
{
  private static readonly string MysqlAdminPath = Path.Combine(
    ConfigurationFile.SERVER_BASE_DIR, "mysql", "bin", "mysqladmin.exe"
  );

  private readonly ILogger<MySqlAdmin> logger = loggerFactory.CreateLogger<MySqlAdmin>();
  private readonly string baseArguments = $"-uroot -pblis123 -h {MySqlIni.MYSQL_BIND_ADDRESS} --port {MySqlIni.MYSQL_PORT}";

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
