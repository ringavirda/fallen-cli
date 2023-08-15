// Vendor namespaces.
using System.Security.Cryptography;
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
    public override bool UseEncryption { get; protected set; } = true;
    public override string PassphraseFile { get; protected set; } = string.Empty;
    public override byte[] Salt { get; protected set; } = new byte[16];

    public DynamicConfig()
        : base()
    {
        // Defaults
        Locale = "en";
        Formatter = KnownFormatters
            .First(format => format.Selector == "inline");

        // Load user settings.
        LoadConfig();

        // Cleanup temporary files.
        TemporaryCleanup();
    }

    /// <summary>
    /// Simple stub to fix json's infinite loading.
    /// </summary>
    /// <param name="Locale">Stored locale.</param>
    /// <param name="Formatter">Stored formatter.</param>
    private record JsonFixture(
        string Locale,
        string Formatter,
        bool UseEncryption,
        string PassphraseFile,
        byte[] Salt);

    /// <summary>
    /// Serializes this object to json.
    /// </summary>
    public override void SaveConfig()
    {
        var json = JsonSerializer.Serialize(new JsonFixture(
            Locale,
            Formatter.Selector,
            UseEncryption,
            PassphraseFile,
            Salt));
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
            UseEncryption = fixture?.UseEncryption ?? true;
            PassphraseFile = fixture?.PassphraseFile ?? string.Empty;
            Salt = fixture?.Salt ?? new byte[16];
        }
        // Save default configs if first launch.
        else SaveConfig();
    }

    /// <summary>
    /// Deletes identity and config files.
    /// </summary>
    public override void PurgeConfig()
    {
        if (File.Exists(ConfigFilePath)) File.Delete(ConfigFilePath);
        if (File.Exists(IdentityFilePath)) File.Delete(IdentityFilePath);
        if (File.Exists(PassphraseFile)) File.Delete(PassphraseFile);
    }

    /// <summary>
    /// Changes locale without validation.
    /// </summary>
    /// <param name="locale">New locale.</param>
    public override void ChangeLocale(string locale)
    {
        Locale = locale;
        // Refresh config.
        SaveConfig();
    }

    /// <summary>
    /// Changes formatter without validation.
    /// </summary>
    /// <param name="formatter">New formatter.</param>
    public override void ChangeFormatter(IConfig.FormatterDescriptor formatter)
    {
        Formatter = formatter;
        // Refresh config.
        SaveConfig();
    }

    /// <summary>
    /// Changes encryption.
    /// </summary>
    /// <param name="encrypt">True if encrypt.</param>
    public override void ChangeEncryption(bool encrypt)
    {
        UseEncryption = encrypt;
        // Refresh config.
        SaveConfig();
    }

    /// <summary>
    /// Changes last passphrase file.
    /// </summary>
    public override void ChangePassphraseFile(string filename)
    {
        PassphraseFile = filename;
        // Refresh config.
        SaveConfig();
    }

    /// <summary>
    /// Regenerates salt.
    /// </summary>
    public override void ChangeSalt()
    {
        using var gen = RandomNumberGenerator.Create();
        gen.GetBytes(Salt);
        // Refresh config.
        SaveConfig();
    }

    /// <summary>
    /// Check and deletes temporary files if enough time has passed.
    /// </summary>
    private void TemporaryCleanup()
    {
        if (!string.IsNullOrEmpty(PassphraseFile))
        {
            if (File.Exists(PassphraseFile)
                && DateTime.Now - File.GetCreationTime(PassphraseFile)
                    > TimeSpan.FromHours(1))
            {
                File.Delete(PassphraseFile);
                PassphraseFile = string.Empty;
            }
        }
        // Refresh config.
        SaveConfig();
    }
}
