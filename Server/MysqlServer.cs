using System.Diagnostics;

namespace BLIS_NG.server;

public class MySqlServer
{
  private readonly string ExePath = Path.Combine(
    Directory.GetCurrentDirectory(),
    "server", "mysql", "bin", "mysql.exe");

  private readonly Process process;

  public MySqlServer()
  {
    process = new Process();
    process.StartInfo.FileName = ExePath;
    process.StartInfo.Arguments = "--help";
    process.StartInfo.WindowStyle = ProcessWindowStyle.Normal;
  }

  public void Start()
  {
    process.Start();
  }

  public void Stop()
  {
    process.WaitForExit();
  }
}
