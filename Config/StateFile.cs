using System.Text.Json;
using System.Text.Json.Serialization;

namespace BLIS_NG.Config;

public class StateFile
{
    private const string FileName = "state.json";
    private const string DefaultVersion = "dev";

    [JsonPropertyName("active_version")]
    public string ActiveVersion { get; set; } = DefaultVersion;

    [JsonPropertyName("previous_version")]
    public string? PreviousVersion { get; set; }

    /// <summary>
    /// Reads state.json from the base directory. Returns defaults if the file is missing or invalid.
    /// </summary>
    public static StateFile Load(string baseDir)
    {
        var path = Path.Combine(baseDir, FileName);
        if (!File.Exists(path))
        {
            return new StateFile();
        }

        try
        {
            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<StateFile>(json) ?? new StateFile();
        }
        catch
        {
            return new StateFile();
        }
    }

    /// <summary>
    /// Writes state.json to the base directory.
    /// </summary>
    public void Save(string baseDir)
    {
        var path = Path.Combine(baseDir, FileName);
        var options = new JsonSerializerOptions { WriteIndented = true };
        var json = JsonSerializer.Serialize(this, options);
        File.WriteAllText(path, json);
    }
}

public class VersionFile
{
    private const string FileName = "version.json";

    [JsonPropertyName("version")]
    public string Version { get; set; } = string.Empty;

    /// <summary>
    /// Reads version.json from the given directory. Returns null if the file is missing or invalid.
    /// </summary>
    public static VersionFile? Load(string directory)
    {
        var path = Path.Combine(directory, FileName);
        if (!File.Exists(path))
        {
            return null;
        }

        try
        {
            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<VersionFile>(json);
        }
        catch
        {
            return null;
        }
    }
}
