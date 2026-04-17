namespace BLIS_NG.Config;

public static class LanguagePreferences
{
    private const string DefaultLanguageCode = "en";
    private const string SettingsFileName = "launcher-language.txt";

    public static string GetLanguageCode()
    {
        try
        {
            var settingsPath = GetSettingsPath();
            if (!File.Exists(settingsPath))
            {
                return DefaultLanguageCode;
            }

            var code = File.ReadAllText(settingsPath).Trim().ToLowerInvariant();
            if (code == "en" || code == "fr")
            {
                return code;
            }
        }
        catch
        {
            // Fall back to English if settings can't be read.
        }

        return DefaultLanguageCode;
    }

    public static void SaveLanguageCode(string languageCode)
    {
        try
        {
            var normalized = languageCode.Trim().ToLowerInvariant();
            if (normalized != "en" && normalized != "fr")
            {
                normalized = DefaultLanguageCode;
            }

            File.WriteAllText(GetSettingsPath(), normalized);
        }
        catch
        {
            // Ignore write failures; app can continue with default language.
        }
    }

    private static string GetSettingsPath() =>
        Path.Combine(ConfigurationFile.ResolveBaseDirectory(), SettingsFileName);
}
