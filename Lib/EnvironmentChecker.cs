using BLIS_NG.Config;

namespace BLIS_NG.Lib;

public static class EnvironmentChecker
{
    public static void CreateRequiredDirectories()
    {
        {
            Directory.CreateDirectory(PhpIni.PHP_SESSION_PATH);
            Directory.CreateDirectory(ConfigurationFile.TMP_DIR);
            Directory.CreateDirectory(ConfigurationFile.LOG_DIR);
        }
    }
}
