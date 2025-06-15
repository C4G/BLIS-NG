using System.Diagnostics;

namespace BLIS_NG.server;

public class MySqlServer
{
  private readonly string ExePath = Path.Combine(
    Directory.GetCurrentDirectory(),
    "server", "mysql", "bin", "mysql.exe");

  private readonly string ConfigPath = Path.Combine(
    Directory.GetCurrentDirectory(),
    "config", "my.cnf"
  );

  private readonly string DataDir = Path.Combine(
    Directory.GetCurrentDirectory(),
    "dbdir"
  );

  private readonly Process process;

  public MySqlServer()
  {
    process = new Process();
    process.StartInfo.FileName = ExePath;
    process.StartInfo.Arguments = $"--defaults-file=\"{ConfigPath}\" --console --datadir=\"{DataDir}\"";
    process.StartInfo.WindowStyle = ProcessWindowStyle.Normal;
  }

  public void Start()
  {
    process.Start();
  }

  public void Stop()
  {
    process.Kill();
    process.WaitForExit();
  }
}
