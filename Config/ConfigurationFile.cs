using System.Text;
using Avalonia.Platform;
using Fluid;

namespace BLIS_NG.Config;

public abstract class ConfigurationFile(Uri templatePath)
{
  public static readonly string SERVER_BASE_DIR = Path.Combine(Directory.GetCurrentDirectory(), "server");
  public static readonly string TMP_DIR = Path.Combine(SERVER_BASE_DIR, "tmp");
  public static readonly string LOG_DIR = Path.Combine(Directory.GetCurrentDirectory(), "log");

  private readonly FluidParser parser = new();
  private readonly Uri templatePath = templatePath;

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
