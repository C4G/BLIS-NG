namespace BLIS_NG.Config;

public class PhpIni : ConfigurationFile
{
    public readonly string PHP_BASE;
    public readonly string PHP_SESSION_PATH;
    private readonly string CONFIG_FILE_PATH;

    public PhpIni() : base(new Uri("avares://BLIS-NG/Assets/Templates/php.ini.liquid"))
    {
        PHP_BASE = Path.Combine(SERVER_BASE_DIR, "php");
        PHP_SESSION_PATH = Path.Combine(SERVER_BASE_DIR, "session");
        if (!Path.Exists(PHP_SESSION_PATH))
        {
            Directory.CreateDirectory(PHP_SESSION_PATH);
        }
        CONFIG_FILE_PATH = Path.Combine(PHP_BASE, "php.ini");
    }

    public override void Write()
    {
        GenerateConfiguration(CONFIG_FILE_PATH, new Dictionary<string, string> {
            { "mysql_port", MySqlIni.MYSQL_PORT.ToString() },
            { "server_dir", SERVER_BASE_DIR },
            { "session_path", PHP_SESSION_PATH },
        });
    }
}
