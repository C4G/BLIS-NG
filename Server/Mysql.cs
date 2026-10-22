using System.Security.Cryptography;
using System.Text;

using BLIS_NG.Config;

using Microsoft.Extensions.Logging;

namespace BLIS_NG.Server;

/// <summary>
/// Process wrapper class for running mysql.exe.
/// Does not run like other processes since it will not continually operate.
/// </summary>
public class MySql(ILogger<MySql> logger, MySqlIni mySqlIni) : BaseProcess(nameof(MySql), logger, singleton: false)
{
    public readonly string MysqlPath = Path.Join(
        mySqlIni.SERVER_BASE_DIR, "mysql", "bin", "mysql.exe"
    );

    private readonly ILogger<MySql> logger = logger;
    private readonly string baseArguments = $"-u{MySqlIni.MYSQL_ROOT_USER} -p{MySqlIni.MYSQL_ROOT_PASSWORD} -h {MySqlIni.MYSQL_BIND_ADDRESS} --port {MySqlIni.MYSQL_PORT}";

    public override void Stop()
    {
        // No-op since this process is not long-running.
        return;
    }

    // ── User password reset ───────────────────────────────────────────────

    /// <summary>
    /// Resets a user's password using the new SHA-256 scheme with a fresh random salt.
    /// </summary>
    public async Task<bool> ResetUserPassword(string username, string cleartextPassword)
    {
        var safeUsername = username.Replace("'", "\\'");
        var newSalt = GenerateSalt();
        var newHash = HashPasswordV2(cleartextPassword, newSalt);

        var sql = $"UPDATE user SET password='{newHash}', salt='{newSalt}' WHERE username='{safeUsername}';";
        var args = $"{baseArguments} blis_revamp -e \"{sql}\"";

        bool hasError = false;
        await Execute(
            MysqlPath, args, null,
            (stdout) => logger.LogInformation("ResetUserPassword stdout: {Message}", stdout),
            (stderr) =>
            {
                if (!stderr.Contains("Using a password on the command line interface"))
                {
                    logger.LogWarning("ResetUserPassword stderr: {Message}", stderr);
                    hasError = true;
                }
            }
        );
        return !hasError;
    }

    /// <summary>
    /// Checks that a user exists with the given credentials and has a level
    /// (supervisor or higher).
    /// This is based off the "level" of the username
    /// </summary>
    public async Task<int?> GetVerifiedUserLevel(string username, string sha1Password)
    {
        var safeUsername = username.Replace("'", "\\'");
        var sql = $"SELECT level FROM user WHERE username='{safeUsername}' AND password='{sha1Password}';";
        var args = $"{baseArguments} blis_revamp -se \"{sql}\"";

        int? level = null;
        await Execute(
            MysqlPath, args, null,
            (stdout) =>
            {
                logger.LogInformation("GetVerifiedUserLevel stdout: {Message}", stdout);
                var lines = stdout.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                var valueLine = lines.Length >= 2 ? lines[1] : lines.Length == 1 ? lines[0] : null;
                if (valueLine != null && int.TryParse(valueLine.Trim(), out var parsed))
                    level = parsed;
            },
            (stderr) =>
            {
                if (!stderr.Contains("Using a password on the command line interface"))
                    logger.LogWarning("GetVerifiedUserLevel stderr: {Message}", stderr);
            }
        );
        return level;
    }

    /// <summary>
    /// Returns the level of a user by username alone, or null if not found.
    /// </summary>
    public async Task<int?> GetUserLevel(string username)
    {
        var safeUsername = username.Replace("'", "\\'");
        var sql = $"SELECT level FROM user WHERE username='{safeUsername}';";
        var args = $"{baseArguments} blis_revamp -se \"{sql}\"";

        int? level = null;
        await Execute(
            MysqlPath, args, null,
            (stdout) =>
            {
                logger.LogInformation("GetUserLevel stdout: {Message}", stdout);
                var lines = stdout.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                var valueLine = lines.Length >= 2 ? lines[1] : lines.Length == 1 ? lines[0] : null;
                if (valueLine != null && int.TryParse(valueLine.Trim(), out var parsed))
                    level = parsed;
            },
            (stderr) =>
            {
                if (!stderr.Contains("Using a password on the command line interface"))
                    logger.LogWarning("GetUserLevel stderr: {Message}", stderr);
            }
        );
        return level;
    }


    // ── Password hashing ──────────────────────────────────────────────────

    /// <summary>
    /// Legacy SHA-1 hash with hardcoded salt. Still used to verify old passwords
    /// that have not yet been upgraded, and for VerifyHigherRankedUser when the
    /// supervisor account has not been upgraded yet.
    /// </summary>
    public static string HashPasswordLegacy(string password)
    {
        var salted = password + "This comment should suffice as salt.";
        var bytes = SHA1.HashData(Encoding.UTF8.GetBytes(salted));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    /// <summary>
    /// New SHA-256 hash with a caller-supplied per-user random salt.
    /// </summary>
    public static string HashPasswordV2(string password, string salt)
    {
        var salted = password + salt;
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(salted));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    /// <summary>
    /// Generates a cryptographically random 64-character hex salt.
    /// </summary>
    public static string GenerateSalt()
    {
        var bytes = new byte[32];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    // ── Supervisor verification ───────────────────────────────────────────

    /// <summary>
    /// Verifies that a supervisor exists, has sufficient rank, and that their
    /// password matches — handling both the new SHA-256 scheme and the legacy
    /// SHA-1 scheme for accounts not yet upgraded.
    ///
    /// TODO: confirm the rank column name in the `user` table with your team
    ///       (e.g. user_type, user_rank, level — check the BLIS schema).
    ///       Based on db_lib.php, 'level' is the column; level >= 2 = Admin.
    /// </summary>
    public async Task<bool> VerifyHigherRankedUser(string username, string cleartextPassword)
    {
        var safeUsername = username.Replace("'", "\\'");

        // Fetch the supervisor's stored hash, salt, and level
        string? storedHash = null;
        string? storedSalt = null;
        int storedLevel = 0;

        var selectSql = $"SELECT password, salt, level FROM user WHERE username='{safeUsername}' LIMIT 1;";
        var selectArgs = $"{baseArguments} blis_revamp -se \"{selectSql}\"";

        await Execute(
            MysqlPath, selectArgs, null,
            (stdout) =>
            {
                logger.LogInformation("VerifyHigherRankedUser fetch stdout: {Message}", stdout);
                // MySQL -se output: header line then data line, tab-separated
                var lines = stdout.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                if (lines.Length >= 2)
                {
                    var cols = lines[1].Split('\t');
                    if (cols.Length >= 3)
                    {
                        storedHash = cols[0].Trim();
                        storedSalt = cols[1].Trim() == "NULL" ? null : cols[1].Trim();
                        int.TryParse(cols[2].Trim(), out storedLevel);
                    }
                }
            },
            (stderr) =>
            {
                if (!stderr.Contains("Using a password on the command line interface"))
                    logger.LogWarning("VerifyHigherRankedUser fetch stderr: {Message}", stderr);
            }
        );

        if (storedHash is null)
        {
            logger.LogWarning("VerifyHigherRankedUser: user '{Username}' not found.", username);
            return false;
        }

        // Level >= 2 = Admin or above (matches $LIS_ADMIN = 2 in db_lib.php)
        if (storedLevel < 2)
        {
            logger.LogWarning("VerifyHigherRankedUser: user '{Username}' has insufficient rank ({Level}).", username, storedLevel);
            return false;
        }

        // Verify password using the appropriate scheme
        bool passwordMatch;
        if (!string.IsNullOrEmpty(storedSalt))
        {
            // New scheme: SHA-256 with per-user salt
            var candidate = HashPasswordV2(cleartextPassword, storedSalt);
            passwordMatch = string.Equals(storedHash, candidate, StringComparison.OrdinalIgnoreCase);
        }
        else
        {
            // Legacy scheme: SHA-1 with hardcoded salt
            var candidate = HashPasswordLegacy(cleartextPassword);
            passwordMatch = string.Equals(storedHash, candidate, StringComparison.OrdinalIgnoreCase);
        }

        if (!passwordMatch)
        {
            logger.LogWarning("VerifyHigherRankedUser: password mismatch for '{Username}'.", username);
        }

        return passwordMatch;
    }
}
