using BLIS_NG.Config;
using BLIS_NG.Lib;

namespace BLIS_NG.Server;

public interface IMainServer
{
    public void Start(Action<MainServer.ServerStatus>? healthcheckAction = null);

    public Task Stop();
}

/// <summary>
/// MainServer encapsulates all the processes and workflow required to start and stop a full instance of BLIS for Windows.
/// </summary>
public class MainServer(MySqlServer mySqlServer, Apache2Server apache2Server, HealthcheckService healthcheckService) : IMainServer
{
    public enum State
    {
        Started,
        Healthy,
        Stopping,
        Stopped,
    }

    public readonly struct ServerStatus
    {
        public readonly State Apache2 { get; init; }
        public readonly State MySql { get; init; }
    }

    private State apache2State = State.Stopped;
    private State mysqlState = State.Stopped;
    private ServerStatus Status { get => new() { Apache2 = apache2State, MySql = mysqlState }; }

    public static Uri ServerUri { get => new($"http://127.0.0.1:{HttpdConf.APACHE2_PORT}/"); }

    private readonly MySqlServer mySqlServer = mySqlServer;
    private Task? mysqlServerTask;

    private readonly Apache2Server apache2Server = apache2Server;
    private Task? apacheServerTask;

    private readonly HealthcheckService healthcheckService = healthcheckService;
    private CancellationTokenSource healthcheckCanceler = new();
    private Task? healthcheck;
    private Action<ServerStatus>? healthcheckAction = null;

    public void Start(Action<ServerStatus>? healthcheckAction = null)
    {
        if (mysqlServerTask == null && !mySqlServer.IsRunning)
        {
            mysqlServerTask = mySqlServer.Run();
            mysqlState = State.Started;
        }

        if (apacheServerTask == null && !apache2Server.IsRunning)
        {
            apacheServerTask = apache2Server.Run();
            apache2State = State.Started;
        }

        this.healthcheckAction = healthcheckAction;

        healthcheck ??= Task.Run(async () =>
          {
              while (!healthcheckCanceler.IsCancellationRequested)
              {
                  await HealthcheckAndUpdateStatus();
                  Thread.Sleep(1000);
              }
          });
    }

    public async Task Stop()
    {
        // TODO: Need some kind of timeout here

        if (healthcheck != null)
        {
            healthcheckCanceler.Cancel();
            await healthcheck;
            healthcheckCanceler = new();
            healthcheck = null;
        }

        apache2State = State.Stopping;
        mysqlState = State.Stopping;
        healthcheckAction?.Invoke(Status);

        if (apacheServerTask != null)
        {
            apache2Server.Stop();
            await apacheServerTask;
            apacheServerTask = null;
        }

        if (mysqlServerTask != null)
        {
            mySqlServer.Stop();
            await mysqlServerTask;
            mysqlServerTask = null;
        }

        await HealthcheckAndUpdateStatus();
    }

    private async Task HealthcheckAndUpdateStatus()
    {
        if (await healthcheckService.MySqlHealthy())
        {
            mysqlState = State.Healthy;
        } else
        {
            mysqlState = State.Stopped;
        }

        if(await healthcheckService.Apache2Healthy())
        {
            apache2State = State.Healthy;
        } else
        {
            apache2State = State.Stopped;
        }

        healthcheckAction?.Invoke(Status);
    }
}
