using BLIS_NG.Config;
using Microsoft.Extensions.Logging;

namespace BLIS_NG.Server;

public class MySqlServer : BaseProcess
{
    public readonly string MysqldPath;
    private readonly string DataDir;
    private readonly string Arguments;

    private readonly ILogger<MySqlServer> logger;
    private readonly MySqlIni mySqlIni;
    private readonly MySqlAdmin mySqlAdmin;
    private readonly MySqlUpgrade mySqlUpgrade;

    public MySqlServer(ILogger<MySqlServer> logger, MySqlIni mySqlIni, MySqlAdmin mySqlAdmin, MySqlUpgrade mySqlUpgrade) : base(nameof(MySqlServer), logger)
    {
        this.logger = logger;
        this.mySqlIni = mySqlIni;
        this.mySqlAdmin = mySqlAdmin;
        this.mySqlUpgrade = mySqlUpgrade;

        MysqldPath = Path.Combine(mySqlIni.SERVER_BASE_DIR, "mysql", "bin", "mysqld.exe");
        DataDir = Path.Combine(mySqlIni.BASE_DIR, "dbdir");

        Arguments = $"--defaults-file=\"{mySqlIni.CONFIG_FILE_PATH}\" --console --datadir=\"{DataDir}\"";

    }

    public async Task<ProcessResult> Run(CancellationToken cancellationToken = default)
    {
        mySqlIni.Write();

        if (UpgradeRequired())
        {
            var result = await PerformUpgrade(cancellationToken);
            if (!result)
            {
                logger.LogError("MySQL upgrade is required but it could not be completed. Check the logs for details.");
                return new ProcessResult(-1);
            }
        }

        return await Execute(MysqldPath, Arguments, null, (stdout) => logger.LogInformation("{StdOut}", stdout), (stderr) => logger.LogWarning("{StdErr}", stderr), cancellationToken);
    }

    public override async void Stop()
    {
        await mySqlAdmin.Shutdown();
    }

    private bool UpgradeRequired()
    {
        if (!File.Exists(Path.Combine(DataDir, "mysql", "plugin.frm")))
        {
            logger.LogWarning("mysql/plugin.frm (mysql.plugin table) does not exist. Attempting to upgrade MySQL database.");
            return true;
        }

        return false;
    }

    private async Task<bool> PerformUpgrade(CancellationToken cancellationToken)
    {
        // Start MySQL server with the --skip-grant-tables option to disable password authentication to enable upgrading
        var args = $"{Arguments} --skip-grant-tables";
        var serverTask = Execute(MysqldPath, args, null, (stdout) => logger.LogInformation("{StdOut}", stdout), (stderr) => logger.LogWarning("{StdErr}", stderr), cancellationToken);

        bool awake = false;
        for (int i = 0; !awake && i < 15; i++)
        {
            // Give the server a second to wake up...
            Thread.Sleep(1000);

            awake = await mySqlAdmin.Ping();
            if (awake) break;
        }

        if (!awake)
        {
            logger.LogError("Could not start MySQL for upgrading.");
            return false;
        }

        await mySqlUpgrade.Run();

        await mySqlAdmin.Shutdown();
        await serverTask;

        return true;
    }
}
