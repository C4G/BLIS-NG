using BLIS_NG.Config;
using BLIS_NG.Server;
using BLIS_NG.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace BLIS_NG.Lib;

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
      .AddSingleton<MySql>()
      .AddSingleton<MySqlUpgrade>()
      .AddSingleton<MySqlServer>()
      .AddSingleton<Apache2Server>()
      .AddSingleton<HealthcheckService>()

      // Main server entrypoint
      .AddSingleton<IMainServer, MainServer>()

      // ViewModels
      .AddSingleton<PasswordResetViewModel>()
      .AddSingleton<ToolsWindowViewModel>()
      .AddSingleton<ServerControlViewModel>()
      .AddSingleton<MainWindowViewModel>();
  }
}
