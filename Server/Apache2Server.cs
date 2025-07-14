using System.Diagnostics;
using BLIS_NG.Config;
using Microsoft.Extensions.Logging;

namespace BLIS_NG.Server;

public class Apache2Server(ILogger<Apache2Server> logger, HttpdConf httpdConf, PhpIni phpIni) : BaseProcess(nameof(Apache2Server), logger)
{
  private static readonly string Apache2Path = Path.Combine(
    HttpdConf.APACHE2_BASE, "bin", "httpd.exe");

  private static readonly string Arguments = $"-f \"{HttpdConf.CONFIG_FILE_PATH}\" -e info";

  private readonly ILogger<Apache2Server> logger = logger;
  private readonly HttpdConf httpdConf = httpdConf;
  private readonly PhpIni phpIni = phpIni;

  public async Task<ProcessResult> Run(CancellationToken cancellationToken = default)
  {
    httpdConf.Write();
    phpIni.Write();

    string path = Environment.GetEnvironmentVariable("PATH") ?? "";
    string newpath = $"{path};{PhpIni.PHP_BASE}";

    var env = new Dictionary<string, string>()
    {
      { "PATH", newpath },
      { "PHPRC", PhpIni.PHP_BASE },
      { "DB_PORT", MySqlIni.MYSQL_PORT.ToString() }
    };

    return await Execute(Apache2Path, Arguments, env, (stdout) => logger.LogInformation("{}", stdout), (stderr) => logger.LogWarning("{}", stderr), cancellationToken);
  }

  public override void Stop()
  {
    // httpd.exe doesn't seem to like clean shutdowns when it's not started as a service.
    // Force it closed here and remove the .pid file.
    Process.Start("taskkill", "/F /IM httpd.exe /T");
    Thread.Sleep(1000);
    File.Delete(Path.Combine(ConfigurationFile.RUN_DIR, "httpd.pid"));
  }
}
