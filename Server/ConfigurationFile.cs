using Fluid;

namespace BLIS_NG.Server;

public abstract class ConfigurationFile(string templatePath)
{
  public static readonly string CONFIG_BASE_DIR = Path.Combine(Directory.GetCurrentDirectory(), "config");
  public static readonly string SERVER_BASE_DIR = Path.Combine(Directory.GetCurrentDirectory(), "server");
  public static readonly string TMP_DIR = Path.Combine(Directory.GetCurrentDirectory(), "tmp");
  public static readonly string RUN_DIR = Path.Combine(Directory.GetCurrentDirectory(), "run");

  private readonly FluidParser parser = new();
  private readonly string templatePath = templatePath;

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
    var template = parser.Parse(File.ReadAllText(templatePath));
    var rendered = RenderTemplate(template, data ?? new Dictionary<string, string>());
    File.WriteAllText(path, rendered);
  }

  public abstract void Write();
}
