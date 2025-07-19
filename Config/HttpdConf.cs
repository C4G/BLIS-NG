namespace BLIS_NG.Config;

public class HttpdConf : ConfigurationFile
{
  // Bind to all interfaces (IPv4) since many labs 
  public const string APACHE2_BIND_ADDRESS = "0.0.0.0";
  public const int APACHE2_PORT = 8080;

  public static readonly string APACHE2_BASE =
    Path.Combine(SERVER_BASE_DIR, "Apache");
  public static readonly string CONFIG_FILE_PATH =
    Path.Combine(APACHE2_BASE, "conf", "httpd.conf");
  public static readonly string DOCROOT =
    Path.Combine(Directory.GetCurrentDirectory(), "htdocs");
  public static readonly string LOG_DIRECTORY =
    Path.Combine(Directory.GetCurrentDirectory(), "log");
  public static readonly string PID_FILE =
    Path.Combine(TMP_DIR, "httpd.pid");

  public HttpdConf() : base(new Uri("avares://BLIS-NG/Assets/Templates/httpd.conf.liquid"))
  { }

  public override void Write()
  {
    GenerateConfiguration(CONFIG_FILE_PATH, new Dictionary<string, string> {
      { "apache_ip", APACHE2_BIND_ADDRESS},
      { "apache_port", APACHE2_PORT.ToString() },
      { "apache_docroot", DOCROOT },
      { "apache_base", APACHE2_BASE },
      { "log_dir", LOG_DIRECTORY },
      { "server_dir", SERVER_BASE_DIR },
      { "pid_file", PID_FILE }
    });
  }
}
