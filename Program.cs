using Avalonia;
using Avalonia.ReactiveUI;

namespace BLIS_NG;

internal class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        try
        {
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }
        catch (Exception e)
        {
            // Output something to stdout to ensure we can debug release builds too.
            Console.WriteLine("[ERROR] " + e.Message);
            throw;
        }
    }


    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
      => AppBuilder.Configure<App>()
          .UsePlatformDetect()
          .LogToTrace()
          .UseReactiveUI();
}
