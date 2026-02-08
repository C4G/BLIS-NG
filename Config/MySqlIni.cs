namespace BLIS_NG.Config;

public class MySqlIni : ConfigurationFile
{
    // Obviously this is not "secure"
    // However, this program is designed to be a drop-in replacement for a program that
    // has been running this way for over a decade. The default root username and password
    // need to be supported here. Perhaps there can be a way to change them in the future.
    // This is local-only software (we only listen on the loopback interface) so the risk is relatively low.
    public const string MYSQL_ROOT_USER = "root";
    public const string MYSQL_ROOT_PASSWORD = "blis123";

    public const string MYSQL_BIND_ADDRESS = "127.0.0.1";
    public const int MYSQL_PORT = 7199;

    public readonly string CONFIG_FILE_PATH;
    public readonly string MYSQL_DBDIR;

    public MySqlIni() : base(new Uri("avares://BLIS-NG/Assets/Templates/my.ini.liquid"))
    {
        CONFIG_FILE_PATH = Path.Combine(SERVER_BASE_DIR, "mysql", "my.ini");
        MYSQL_DBDIR = Path.Combine(BASE_DIR, "dbdir");
    }

    public override void Write()
    {
        GenerateConfiguration(CONFIG_FILE_PATH, new Dictionary<string, string> {
            { "mysql_ip", MYSQL_BIND_ADDRESS },
            { "mysql_port", MYSQL_PORT.ToString() },
            { "server_dir", SERVER_BASE_DIR.Replace("\\", "\\\\") },
            { "mysql_datadir", MYSQL_DBDIR.Replace("\\", "\\\\") },
            { "tmp_dir", TMP_DIR.Replace("\\", "\\\\") }
        });
    }
}
