using BLIS_NG.Server;
using Microsoft.Extensions.Logging;

namespace BLIS_NG.server;

public class Apache2Server(ILoggerFactory loggerFactory) : BaseProcess(nameof(Apache2Server), loggerFactory)
{
  private static readonly string Apache2Path = Path.Combine(
    HttpdConf.APACHE2_BASE, "bin", "httpd.exe");

  private static readonly string ConfigPath = Path.Combine(
    ConfigurationFile.RUN_DIR, "httpd.conf"
  );

  private static readonly string Arguments = $"-f \"{ConfigPath}\"";

  private readonly HttpdConf httpdConf = new();

  public override async Task<ProcessResult> Run(Action<string>? stdOutConsumer = null, Action<string>? stdErrConsumer = null, CancellationToken cancellationToken = default)
  {
    httpdConf.Write();
    return await Execute(Apache2Path, Arguments, stdOutConsumer, stdErrConsumer, cancellationToken);
  }

  public override void Stop()
  {
    Kill();
  }
}
