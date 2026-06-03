using System.Text.Json;
using System.Text.Json.Serialization;

namespace BLIS_NG.Config;

public enum LanguagePreference
{
    EN,
    FR
}

public static class LanguagePreferenceExtensions
{
    public static string DisplayName(this LanguagePreference languagePreference)
    {
        return languagePreference switch
        {
            LanguagePreference.EN => "English",
            LanguagePreference.FR => "French",
            _ => string.Empty,
        };
    }

    public static string LanguageCode(this LanguagePreference languagePreference)
    {
        return Enum.GetName(languagePreference)?.ToLowerInvariant() ?? string.Empty;
    }
}

public class AppSettings
{
    public const string LauncherSettings = nameof(LauncherSettings);

    public LanguagePreference Language { get; set; } = LanguagePreference.EN;
    public bool OpenBrowserOnStart { get; set; } = true;

    private static readonly JsonSerializerOptions jsonSerializerOptions = new()
    {
        WriteIndented = true,
        Converters = {
            new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)
        }
    };

    public void Write()
    {
        var settings = new Dictionary<string, object>
        {
            [LauncherSettings] = this
        };
        var serialized = JsonSerializer.Serialize(settings, jsonSerializerOptions);
        File.WriteAllText(ConfigPath(), serialized);
    }

    /// <summary>
    /// Returns the location of the appsettings.json file.
    /// If running as the .NET Debugger, this will be in the current working directory.
    /// If not, this will be the same folder as the current executable.
    /// </summary>
    /// <returns></returns>
    public static string ConfigPath()
    {
        const string filename = "appsettings.json";

        var procname = Path.GetFileName(Environment.ProcessPath)?.ToLowerInvariant();
        var runningAsDebugger = procname?.StartsWith("dotnet") == true;

        if (!runningAsDebugger)
        {
            return Path.Combine(Path.GetFileName(Environment.ProcessPath) ?? string.Empty, filename);
        }

        return Path.Combine(Directory.GetCurrentDirectory(), filename);
    }

    private static string? workingDirectory = null;
    public static string ResolveBaseDirectory()
    {
        if (workingDirectory != null)
        {
            return workingDirectory;
        }

        var args = Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length; i++)
        {
            if (string.Equals(args[i], "--WorkingDirectory") && i < (args.Length - 1))
            {
                workingDirectory = Path.GetFullPath(new Uri(args[i + 1]).LocalPath);
                break;
            }
        }

        if (workingDirectory == null || !Path.Exists(workingDirectory))
        {
            workingDirectory = Directory.GetCurrentDirectory();
        }

        Console.WriteLine($"Working directory: {workingDirectory}");

        return workingDirectory;
    }
}
