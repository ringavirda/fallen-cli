// FCli namespaces.
using FCli.Models.Types;
using FCli.Services.Abstractions;
using FCli.Services.Format;
using FCli.Services.Tools;

namespace FCli.Services.Config;

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
    public List<string> KnownLocales => new()
    {
        "en", "en-US", "ru", "ru-RU", "uk", "uk-UA"
    };
    public List<IConfig.FormatterDescriptor> KnownFormatters => new()
    {
        new("inline", typeof(InlineFormatter)),
        new("pretty", typeof(PrettyFormatter)),
    };
#pragma warning disable 8625
    public List<IToolDescriptor> KnownTools => new()
    {
        new AddTool(null, null, null, null, null),
        new ChangeTool(null, null, null, null, null),
        new ConfigTool(null, null, null),
        new GroupTool(null, null, null, null, null),
        new ListTool( null, null, null, null),
        new RemoveTool(null, null, null),
        new RunTool(null, null, null, null),
    };
#pragma warning restore 8625
    public List<IConfig.CommandDescriptor> KnownCommands => new()
    {
        new("exe", CommandType.Executable, false, "exe"),
        new("url", CommandType.Website, false, null),
        new("script", CommandType.Script, true, null),
        new("dir", CommandType.Directory, false, null),
        new("shell", CommandType.Shell, true, null)
    };
    public List<IConfig.ShellDescriptor> KnownShells => new()
    {
        new("bash", ShellType.Bash, "sh"),
        new("cmd", ShellType.Cmd, "bat"),
        new("powershell", ShellType.Powershell, "ps1"),
        new("fish", ShellType.Fish, "fish")
    };
    public string StringsResourceLocation => "FCli.Resources.Strings";

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

        StorageFileName = "storage-x.json";
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

        StorageFileName = "storage-x.json";
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
    
    public abstract string Locale { get; protected set; }
    public abstract IConfig.FormatterDescriptor Formatter { get; protected set; }

    public abstract void SaveConfig();
    public abstract void LoadConfig();
    public abstract void PurgeConfig();
    public abstract void ChangeLocale(string locale);
    public abstract void ChangeFormatter(IConfig.FormatterDescriptor formatter);
}
