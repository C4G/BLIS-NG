using BLIS_NG.Config;
using BLIS_NG.Lib;
using BLIS_NG.Server;
using BLIS_NG.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace BLIS_NG;

public static class ServiceCollectionExtensions
{
  public static IServiceCollection AddDependencies(this IServiceCollection services)
  {
    return services
      // Configuration
      .AddSingleton<HttpdConf>()
      .AddSingleton<MySqlIni>()
      .AddSingleton<PhpIni>()

      // Server & utility processes
      .AddSingleton<MySqlAdmin>()
      .AddSingleton<MySqlUpgrade>()
      .AddSingleton<MySqlServer>()
      .AddSingleton<Apache2Server>()
      .AddSingleton<HealthcheckService>()

      // ViewModels
      .AddSingleton<ServerControlViewModel>()
      .AddSingleton<MainWindowViewModel>();
  }
}
