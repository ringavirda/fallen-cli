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

    public string IdentityFileName { get; private set; }

    public string IdentityFilePath { get; private set; }

    public List<string> KnownLocales => new()
    {
        "en", "en-US", "ru", "ru-RU", "uk", "uk-UA"
    };
    public List<IConfig.FormatterDescriptor> KnownFormatters => new()
    {
        new("inline", typeof(InlineFormatter)),
        new("pretty", typeof(PrettyFormatter)),
    };

    public List<IToolDescriptor> KnownTools => new()
    {
        new AddTool(),
        new ChangeTool(),
        new ConfigTool(),
        new GroupTool(),
        new ListTool(),
        new RemoveTool(),
        new RunTool(),
        new PrimesTool(),
        new MailTool(),
        new IdentityTool()
    };
    
    public List<IConfig.CommandDescriptor> KnownCommands => new()
    {
        new("exe", CommandType.Executable, false, "exe"),
        new("url", CommandType.Website, false, null),
        new("script", CommandType.Script, true, null),
        new("dir", CommandType.Directory, false, null)
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

        // Configure general constants.
        StorageFileName = "storage-x.json";
        StorageFilePath = Path.Combine(
            AppFolderPath,
            StorageFileName);

        ConfigFileName = "config.json";
        ConfigFilePath = Path.Combine(AppFolderPath, ConfigFileName);



        IdentityFileName = "identity.json";
        IdentityFilePath = Path.Combine(
            AppFolderPath,
            IdentityFileName);

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

        LogsFolderName = "Logs";
        LogsFileTemplate = "FCli.log";
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

        LogsFolderName = "logs";
        LogsFileTemplate = "fcli-log.log";
        LogsPath = Path.Combine(
            AppFolderPath,
            LogsFolderName,
            LogsFileTemplate);
    }

    // Pass down to the dynamic config.

    public abstract string Locale { get; protected set; }
    public abstract IConfig.FormatterDescriptor Formatter { get; protected set; }
    public abstract bool UseEncryption { get; protected set; }
    public abstract string PassphraseFile { get; protected set; }
    public abstract byte[] Salt { get; protected set; }

    public abstract void SaveConfig();
    public abstract void LoadConfig();
    public abstract void PurgeConfig();
    public abstract void ChangeLocale(string locale);
    public abstract void ChangeFormatter(IConfig.FormatterDescriptor formatter);
    public abstract void ChangeEncryption(bool ifEncrypt);
    public abstract void ChangePassphraseFile(string filename);
    public abstract void ChangeSalt();
}
