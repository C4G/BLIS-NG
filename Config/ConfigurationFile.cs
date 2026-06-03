using System.Text;
using Avalonia.Platform;
using Fluid;

namespace BLIS_NG.Config;

public abstract class ConfigurationFile
{
    public readonly string BASE_DIR;
    public readonly string SERVER_BASE_DIR;
    public readonly string TMP_DIR;
    public readonly string LOG_DIR;

    private readonly FluidParser parser = new();
    private readonly Uri templatePath;

    public ConfigurationFile(Uri templatePath)
    {
        this.templatePath = templatePath;

        BASE_DIR = AppSettings.ResolveBaseDirectory();
        SERVER_BASE_DIR = Path.Join(BASE_DIR, "server");

        TMP_DIR = Path.Join(SERVER_BASE_DIR, "tmp");
        if (!Path.Exists(TMP_DIR))
        {
            Directory.CreateDirectory(TMP_DIR);
        }

        LOG_DIR = Path.Join(BASE_DIR, "log");
        if (!Path.Exists(LOG_DIR))
        {
            Directory.CreateDirectory(LOG_DIR);
        }
    }

    private static string RenderTemplate(IFluidTemplate template, IDictionary<string, string> data)
    {
        TemplateContext context = new();
        foreach (var (key, value) in data)
        {
            context.SetValue(key, value);
        }

        return template.Render(context);
    }

    protected void GenerateConfiguration(string path, IDictionary<string, string>? data = null)
    {
        var templateContents = ReadTemplate(templatePath);
        var template = parser.Parse(templateContents);
        var rendered = RenderTemplate(template, data ?? new Dictionary<string, string>());
        File.WriteAllText(path, rendered);
    }

    public abstract void Write();

    private static string ReadTemplate(Uri path)
    {
        var templateBuilder = new StringBuilder();

        using var stream = AssetLoader.Open(path);
        using var streamReader = new StreamReader(stream);
        while (!streamReader.EndOfStream)
        {
            var line = streamReader.ReadLine();
            if (line != null)
            {
                templateBuilder
                  .Append(line)
                  .AppendLine();
            }
        }

        return templateBuilder.ToString();
    }
}
