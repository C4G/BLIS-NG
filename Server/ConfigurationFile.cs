using Fluid;

namespace BLIS_NG.Server;

public abstract class ConfigurationFile
{
  public static readonly string CONFIG_BASE_DIR = Path.Combine(Directory.GetCurrentDirectory(), "config");
  public static readonly string SERVER_BASE_DIR = Path.Combine(Directory.GetCurrentDirectory(), "server");
  public static readonly string TMP_DIR = Path.Combine(Directory.GetCurrentDirectory(), "tmp");
  public static readonly string RUN_DIR = Path.Combine(Directory.GetCurrentDirectory(), "run");

  private readonly FluidParser parser = new();
  private readonly IFluidTemplate template;

  public ConfigurationFile(string templatePath)
  {
    template = parser.Parse(File.ReadAllText(templatePath));
  }

  private string RenderTemplate(IDictionary<string, string> data)
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
    var rendered = RenderTemplate(data ?? new Dictionary<string, string>());
    File.WriteAllText(path, rendered);
  }

  public abstract void Write();
}
