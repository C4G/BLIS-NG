using Microsoft.Extensions.Logging;

namespace BLIS_NG.Config;

public class AppConfig
{
  public static ILoggerFactory CreateLoggerFactory()
  {
    return LoggerFactory.Create(builder =>
    {
      builder.AddDebug();
    });
  }
}
