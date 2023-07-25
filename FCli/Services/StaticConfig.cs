using FCli.Services.Format;

namespace FCli.Services;

/// <summary>
/// Encapsulates the static layer of the config.
/// </summary>
public abstract class StaticConfig : IConfig
{
    public string AppFolderName { get; private set; }
    public string AppFolderPath { get; private set; }
    public string StorageFileName { get; private set; }
    public string StorageFilePath { get; private set; }
    public string ConfigFileName { get; private set; }
    public string ConfigFilePath { get; private set; }
    public string LogsFileTemplate { get; private set; }
    public string LogsFolderName { get; private set; }
    public string LogsPath { get; private set; }
    public Dictionary<string, Type> KnownFormatters => new() {
        { "inline", typeof(InlineFormatter)},
        { "pretty", typeof(PrettyFormatter)},
    };
    public List<string> KnownLocales => new() 
    {
        "en", "ru", "ua"
    };

#pragma warning disable 8618, 8604
    public StaticConfig()
    {
        if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            ConfigureWindows();
        else if (Environment.OSVersion.Platform == PlatformID.Unix)
            ConfigureUnix();
        else throw new PlatformNotSupportedException(
            "FCli supports only WinNT and Unix based systems.");

        // Guard against uninitialized directory.
        if (!Directory.Exists(AppFolderPath))
            Directory.CreateDirectory(AppFolderPath);
    }
#pragma warning restore 8618, 8604

    /// <summary>
    /// Set up dynamic configuration for Windows.
    /// </summary>
    private void ConfigureWindows()
    {
        AppFolderName = "FCli";
        AppFolderPath = Path.Combine(
            Environment.GetFolderPath(
                Environment.SpecialFolder.ApplicationData),
            AppFolderName);

        StorageFileName = "storage.json";
        StorageFilePath = Path.Combine(
            AppFolderPath,
            StorageFileName);

        ConfigFileName = "config.json";
        ConfigFilePath = Path.Combine(AppFolderPath, ConfigFileName);

        LogsFolderName = "Logs";
        LogsFileTemplate = "fcli-log.log";
        LogsPath = Path.Combine(
            AppFolderPath,
            LogsFolderName,
            LogsFileTemplate);
    }

    /// <summary>
    /// Set up dynamic configuration for Linux.
    /// </summary>
    private void ConfigureUnix()
    {
        AppFolderName = ".fcli";
        AppFolderPath = Path.Combine(
            Environment.GetFolderPath(
                Environment.SpecialFolder.Personal),
            ".fcli");

        StorageFileName = "storage.json";
        StorageFilePath = Path.Combine(
            AppFolderPath,
            StorageFileName);

        ConfigFileName = "config.json";
        ConfigFilePath = Path.Combine(AppFolderPath, ConfigFileName);

        LogsFolderName = "logs";
        LogsFileTemplate = "fcli-log.log";
        LogsPath = Path.Combine(
            AppFolderPath,
            LogsFolderName,
            LogsFileTemplate);
    }

    // Pass down the hierarchy.
    public abstract string Locale { get; protected set;}
    public abstract string Formatter { get; protected set; }

    public abstract void SaveConfig();
    public abstract void LoadConfig();
    public abstract void PurgeConfig();
    public abstract void ChangeLocale(string locale);
    public abstract void ChangeFormatter(string formatter);
}