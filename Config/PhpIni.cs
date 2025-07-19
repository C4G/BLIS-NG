namespace BLIS_NG.Config;

public class PhpIni : ConfigurationFile
{
  public static readonly string PHP_BASE =
    Path.Combine(SERVER_BASE_DIR, "php");
  public static readonly string PHP_SESSION_PATH =
    Path.Combine(SERVER_BASE_DIR, "session");
  private static readonly string CONFIG_FILE_PATH =
    Path.Combine(PHP_BASE, "php.ini");

  public PhpIni() : base(new Uri("avares://BLIS-NG/Assets/Templates/php.ini.liquid"))
  { }

  public override void Write()
  {
    GenerateConfiguration(CONFIG_FILE_PATH, new Dictionary<string, string> {
      { "mysql_port", MySqlIni.MYSQL_PORT.ToString() },
      { "server_dir", SERVER_BASE_DIR },
      { "session_path", PHP_SESSION_PATH },
    });
  }
}
