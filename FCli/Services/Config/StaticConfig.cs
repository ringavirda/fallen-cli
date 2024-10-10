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
#pragma warning disable 8618
    public string StorageFileName { get; protected set; }
    public string StorageFilePath { get; protected set; }
    public string ConfigFileName { get; protected set; }
    public string ConfigFilePath { get; protected set; }
    public string IdentityFileName { get; protected set; }
    public string IdentityFilePath { get; protected set; }
    public string AppFolderName { get; protected set; }
    public string AppFolderPath { get; protected set; }
    public string LogsFileTemplate { get; protected set; }
    public string LogsFolderName { get; protected set; }
    public string LogsPath { get; protected set; }
    public string PassphraseFile { get; protected set; }
    public string Locale { get; protected set; }
    public bool UseEncryption { get; protected set; }
    public byte[] Salt { get; protected set; }
    public IConfig.FormatterDescriptor Formatter { get; protected set; }
# pragma warning restore 8618

    public List<string> KnownLocales =>
    [
        "en", "en-US", "ru", "ru-RU", "uk", "uk-UA"
    ];
    public List<IConfig.FormatterDescriptor> KnownFormatters =>
    [
        new("inline", typeof(InlineFormatter)),
        new("pretty", typeof(PrettyFormatter)),
    ];

    public List<IToolDescriptor> KnownTools =>
    [
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
    ];

    public List<IConfig.CommandDescriptor> KnownCommands =>
    [
        new("exe", CommandType.Executable, false, ".exe"),
        new("url", CommandType.Website, false, null),
        new("script", CommandType.Script, true, null),
        new("dir", CommandType.Directory, false, null)
    ];
    public List<IConfig.ShellDescriptor> KnownShells =>
    [
        new("bash", ShellType.Bash, ".sh"),
        new("cmd", ShellType.Cmd, ".bat"),
        new("powershell", ShellType.Powershell, ".ps1"),
        new("fish", ShellType.Fish, ".fish")
    ];
    public string StringsResourceLocation => "FCli.Resources.Strings";

    // Pass down to the dynamic config.
    public abstract void SaveConfig();
    public abstract void LoadConfig();
    public abstract void PurgeConfig();
    public abstract void ChangeLocale(string locale);
    public abstract void ChangeFormatter(IConfig.FormatterDescriptor formatter);
    public abstract void ChangeEncryption(bool encrypt);
    public abstract void ChangePassphraseFile(string filename);
    public abstract void ChangeSalt();
    public abstract void ChangeAppFolder(DirectoryInfo? directory);
}