using BLIS_NG.Config;
using Microsoft.Extensions.Logging;

namespace BLIS_NG.Server;

/// <summary>
/// Process wrapper class for running mysqladmin.exe.
/// Does not run like other processes since it will not continually operate.
/// </summary>
public class MySqlAdmin(ILogger<MySqlAdmin> logger, MySqlIni mySqlIni) : BaseProcess(nameof(MySqlAdmin), logger, singleton: false)
{
    public readonly string MysqlAdminPath = Path.Combine(
        mySqlIni.SERVER_BASE_DIR, "mysql", "bin", "mysqladmin.exe"
    );

    public readonly string MysqlPath = Path.Combine(
        mySqlIni.SERVER_BASE_DIR, "mysql", "bin", "mysql.exe"
    );

    private readonly ILogger<MySqlAdmin> logger = logger;
    private readonly string baseArguments = $"-u{MySqlIni.MYSQL_ROOT_USER} -p{MySqlIni.MYSQL_ROOT_PASSWORD} -h {MySqlIni.MYSQL_BIND_ADDRESS} --port {MySqlIni.MYSQL_PORT}";

    public override void Stop()
    {
        // No-op since this process is not long-running.
        return;
    }

    public async Task<bool> Ping()
    {
        // Not logging stdout here since it will just fill up logs.
        var result = await Execute(MysqlAdminPath, $"{baseArguments} ping", null, null, null);
        return result.ExitCode == 0;
    }

    public async Task Shutdown()
    {
        await Execute(MysqlAdminPath, $"{baseArguments} shutdown", null,
            (stdout) => logger.LogInformation("{Message}", stdout),
            (stderr) => logger.LogWarning("{Message}", stderr));
    }

    public async Task<bool> ResetUserPassword(string username, string sha1Password)
    {
        var safeUsername = username.Replace("'", "\\'");
        var sql  = $"UPDATE user SET password='{sha1Password}' WHERE username='{safeUsername}';";
        var args = $"{baseArguments} blis_revamp -e \"{sql}\"";

        bool hasError = false;
        await Execute(
            MysqlPath, args, null,
            (stdout) => logger.LogInformation("ResetUserPassword stdout: {Message}", stdout),
            (stderr) =>
            {
                logger.LogWarning("{Message}", stderr);
                // The password warning is harmless, ignore it
                if (!stderr.Contains("Using a password on the command line interface"))
                    hasError = true;
            }
        );
        return !hasError;
    }

    /// <summary>
    /// Checks that a user exists with the given credentials and has a rank >= 2
    /// (supervisor or higher).
    /// TODO: confirm the rank column name in the `user` table with your team
    /// (e.g. user_type, user_rank, rank — check the BLIS schema).
    /// </summary>
    public async Task<bool> VerifyHigherRankedUser(string username, string sha1Password)
    {
        var safeUsername = username.Replace("'", "\\'");
        var sql  = $"SELECT COUNT(*) FROM user WHERE username='{safeUsername}' AND password='{sha1Password}' AND user_rank >= 2;";
        var args = $"{baseArguments} blis_revamp -se \"{sql}\"";

        bool found = false;
        await Execute(
            MysqlPath, args, null,
            (stdout) =>
            {
                logger.LogInformation("VerifyHigherRankedUser stdout: {Message}", stdout);
                var lines = stdout.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                if (lines.Length >= 2 && int.TryParse(lines[1].Trim(), out var count))
                    found = count > 0;
            },
            (stderr) =>
            {
                if (!stderr.Contains("Using a password on the command line interface"))
                    logger.LogWarning("VerifyHigherRankedUser stderr: {Message}", stderr);
            }
        );
        return found;
    }
}
