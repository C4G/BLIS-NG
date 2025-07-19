using System.Net.Http.Json;
using BLIS_NG.Config;
using BLIS_NG.Server;
using Microsoft.Extensions.Logging;

namespace BLIS_NG.Lib;

public class HealthcheckService(ILogger<HealthcheckService> logger, MySqlAdmin mySqlAdmin)
{
  private readonly ILogger<HealthcheckService> logger = logger;
  private readonly MySqlAdmin mySqlAdmin = mySqlAdmin;

  private static readonly HttpClient httpClient = new()
  {
      BaseAddress = new Uri($"http://127.0.0.1:{HttpdConf.APACHE2_PORT}/"),
  };

  private class HealthcheckResponse {
    public required string Status { get; set; }
  }

  public async Task<bool> MySqlHealthy()
  {
    return await mySqlAdmin.Ping();
  }

  public async Task<bool> Apache2Healthy()
  {
    try
    {
      var response = await httpClient.GetAsync("/healthcheck.php");
      var responseBody = await response.Content.ReadFromJsonAsync<HealthcheckResponse>();
      if (responseBody?.Status == "ok")
      {
        return true;
      }

      logger.LogWarning("Apache2 is not healthy. Response status code: {StatusCode}, error message: {ErrorMessage}", response.StatusCode, responseBody?.Status);
      return false;
    }
    catch (Exception e)
    {
      logger.LogWarning("Apache2 is not healthy: {Message}", e.Message);
      return false;
    }
  }
}
