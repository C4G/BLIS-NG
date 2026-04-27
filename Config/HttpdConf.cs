namespace BLIS_NG.Config;

public class HttpdConf : ConfigurationFile
{
    // Bind to all interfaces (IPv4) since many labs 
    public const string APACHE2_BIND_ADDRESS = "0.0.0.0";
    public const int APACHE2_PORT = 8080;

    public readonly string APACHE2_BASE;
    public readonly string CONFIG_FILE_PATH;
    public readonly string DOCROOT;
    public readonly string PID_FILE;
    public readonly string LOCAL_DIR;
    public readonly string STORAGE_DIR;
    public readonly string DATA_DIR;

    public HttpdConf() : base(new Uri("avares://BLIS-NG/Assets/Templates/httpd.conf.liquid"))
    {
        APACHE2_BASE = Path.Join(SERVER_BASE_DIR, "Apache");
        CONFIG_FILE_PATH = Path.Join(APACHE2_BASE, "conf", "httpd.conf");

        var state = StateFile.Load(BASE_DIR);
        DOCROOT = Path.Join(BASE_DIR, state.EffectiveDocroot);

        PID_FILE = Path.Join(TMP_DIR, "httpd.pid");
        LOCAL_DIR = Path.Join(BASE_DIR, "local");
        STORAGE_DIR = Path.Join(BASE_DIR, "storage");
        DATA_DIR = Path.Join(BASE_DIR, "data");
    }

    public override void Write()
    {
        GenerateConfiguration(CONFIG_FILE_PATH, new Dictionary<string, string> {
            { "apache_ip", APACHE2_BIND_ADDRESS},
            { "apache_port", APACHE2_PORT.ToString() },
            { "apache_docroot", DOCROOT },
            { "apache_base", APACHE2_BASE },
            { "log_dir", LOG_DIR },
            { "server_dir", SERVER_BASE_DIR },
            { "pid_file", PID_FILE },
            { "local_dir", LOCAL_DIR },
            { "storage_dir", STORAGE_DIR },
            { "data_dir", DATA_DIR }
        });
    }
}
