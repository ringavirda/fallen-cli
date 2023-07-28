// Vendor namespaces.
using System.Text.Json;
// FCli namespaces.
using FCli.Services.Abstractions;

namespace FCli.Services.Config;

/// <summary>
/// Encapsulates the part of the config that can be changed by the user.
/// </summary>
public class DynamicConfig : StaticConfig
{
    // Inline defaults.
    public override string Locale { get; protected set; }
    public override IConfig.FormatterDescriptor Formatter { get; protected set; }

    public DynamicConfig()
        : base()
    {
        // Defaults
        Locale = "en";
        Formatter = KnownFormatters
            .First(format => format.Selector == "inline");

        // Load user settings.
        LoadConfig();
    }

    /// <summary>
    /// Simple stub to fix json's infinite loading.
    /// </summary>
    /// <param name="Locale">Stored locale.</param>
    /// <param name="Formatter">Stored formatter.</param>
    private record JsonFixture(
        string Locale,
        string Formatter);

    /// <summary>
    /// Serializes this object to json.
    /// </summary>
    public override void SaveConfig()
    {
        var json = JsonSerializer.Serialize(new JsonFixture(
            Locale,
            Formatter.Selector));
        File.WriteAllText(ConfigFilePath, json);
    }
    
    /// <summary>
    /// Loads user config from storage and deserializes it.
    /// </summary>
    public override void LoadConfig()
    {
        if (File.Exists(ConfigFilePath))
        {
            var json = File.ReadAllText(ConfigFilePath);
            JsonFixture? fixture = null;
            if (!string.IsNullOrEmpty(json))
                fixture = JsonSerializer.Deserialize<JsonFixture>(json);
            Locale = fixture?.Locale ?? Locale;
            Formatter = KnownFormatters
                .FirstOrDefault(format => format.Selector == fixture?.Formatter) 
                ?? Formatter;
        }
        // Save default configs if first launch.
        else SaveConfig();
    }

    /// <summary>
    /// Deletes saved config file.
    /// </summary>
    public override void PurgeConfig()
    {
        if (File.Exists(ConfigFilePath))
            File.Delete(ConfigFilePath);
    }

    /// <summary>
    /// Changes locale without validation.
    /// </summary>
    /// <param name="locale">New locale.</param>
    public override void ChangeLocale(string locale)
    {
        Locale = locale;
        SaveConfig();
    }
    
    /// <summary>
    /// Changes formatter without validation.
    /// </summary>
    /// <param name="formatter">New formatter.</param>
    public override void ChangeFormatter(IConfig.FormatterDescriptor formatter)
    {
        Formatter = formatter;
        SaveConfig();
    }
}
