using System.Globalization;
using System.Reflection;
using System.Resources;

namespace BLIS_NG.Lang;

public static class Resources
{
    private static readonly ResourceManager ResourceManager = new(
        "BLIS_NG.Lang.Resources",
        Assembly.GetExecutingAssembly());

    public static CultureInfo? Culture { get; set; }

    private static string Get(string key, string fallback) =>
        ResourceManager.GetString(key, Culture) ?? fallback;

    public static string App_Title => Get(nameof(App_Title), "Basic Lab Information System (BLIS)");
    public static string App_Version_Format => Get(nameof(App_Version_Format), "BLIS for Windows {0}");
    public static string App_Tagline_Line1 => Get(nameof(App_Tagline_Line1), "A Joint Initiative of");
    public static string App_Tagline_Line2 => Get(nameof(App_Tagline_Line2), "C4G at Georgia Tech, the CDC, and participating countries.");
    public static string App_LicenseNotice => Get(nameof(App_LicenseNotice), "C4G BLIS has been licensed under the GNU General Public License version 3. For more information, visit blis.cc.gatech.edu.");
    public static string Button_StartBlis => Get(nameof(Button_StartBlis), "Start BLIS");
    public static string Button_StopBlis => Get(nameof(Button_StopBlis), "Stop BLIS");
    public static string Status_Healthy => Get(nameof(Status_Healthy), "Status: Healthy");
    public static string Status_Starting => Get(nameof(Status_Starting), "Status: Starting");
    public static string Status_ApacheHealthcheckFailed => Get(nameof(Status_ApacheHealthcheckFailed), "Status: Apache2 health check failed.");
    public static string Status_Stopping => Get(nameof(Status_Stopping), "Status: Stopping");
    public static string Status_Stopped => Get(nameof(Status_Stopped), "Status: Stopped");
}
