using System.Diagnostics;
using BLIS_NG.Config;
using Microsoft.Extensions.Logging;

namespace BLIS_NG.Server;

public class Apache2Server(ILogger<Apache2Server> logger, HttpdConf httpdConf, PhpIni phpIni) : BaseProcess(nameof(Apache2Server), logger)
{
    public readonly string Apache2Path = Path.Combine(
      httpdConf.APACHE2_BASE, "bin", "httpd.exe");

    private readonly string Arguments = $"-f \"{httpdConf.CONFIG_FILE_PATH}\" -e info";

    private readonly ILogger<Apache2Server> logger = logger;
    private readonly HttpdConf httpdConf = httpdConf;
    private readonly PhpIni phpIni = phpIni;

    public async Task<ProcessResult> Run(CancellationToken cancellationToken = default)
    {
        httpdConf.Write();
        phpIni.Write();

        string path = Environment.GetEnvironmentVariable("PATH") ?? "";
        string newpath = $"{path};{phpIni.PHP_BASE}";

        var env = new Dictionary<string, string>()
    {
      { "PATH", newpath },
      { "PHPRC", phpIni.PHP_BASE },
      { "DB_PORT", MySqlIni.MYSQL_PORT.ToString() }
    };

        return await Execute(Apache2Path, Arguments, env, (stdout) => logger.LogInformation("{StdOut}", stdout), (stderr) => logger.LogWarning("{StdErr}", stderr), cancellationToken);
    }

    public override void Stop()
    {
        // httpd.exe doesn't seem to like clean shutdowns when it's not started as a service.
        // Force it closed here and remove the .pid file.
        Process.Start("taskkill", "/F /IM httpd.exe /T");
        Thread.Sleep(1000);
        File.Delete(httpdConf.PID_FILE);
    }
}
