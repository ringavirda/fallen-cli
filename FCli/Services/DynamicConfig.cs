using System.Text.Json;

namespace FCli.Services;

/// <summary>
/// Encapsulates the part of the config that can be changed by the user.
/// </summary>
public class DynamicConfig : StaticConfig
{
    public override string Locale { get; protected set; } = "en";
    public override string Formatter { get; protected set; } = "inline";

    public DynamicConfig()
    {
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
            Formatter));
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
            Formatter = fixture?.Formatter ?? Formatter;
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
    public override void ChangeFormatter(string formatter)
    {
        Formatter = formatter;
        SaveConfig();
    }
}