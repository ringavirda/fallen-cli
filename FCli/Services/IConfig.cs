using System.Globalization;

namespace FCli.Services;

public interface IConfig
{
    // Static configs.

    /// <summary>
    /// Name for the root app folder.
    /// </summary>
    public string AppFolderName { get; }
    /// <summary>
    /// Root app path.
    /// </summary>
    public string AppFolderPath { get; }
    /// <summary>
    /// Name for the command storage file.
    /// </summary>
    public string StorageFileName { get; }
    /// <summary>
    /// Path to the command storage file.
    /// </summary>
    public string StorageFilePath { get; }
    /// <summary>
    /// Name of the file that stores this object.
    /// </summary>
    public string ConfigFileName { get; }
    /// <summary>
    /// Path to the configuration file.
    /// </summary>
    public string ConfigFilePath { get; }
    /// <summary>
    /// Template for log file names.
    /// </summary>
    public string LogsFileTemplate { get; }
    /// <summary>
    /// Name for the folder that contains logs.
    /// </summary>
    public string LogsFolderName { get; }
    /// <summary>
    /// Path to the logs template.
    /// </summary>
    public string LogsPath { get; }
    /// <summary>
    /// Return pairs of formatter-selector and formatter-type.
    /// </summary>
    public Dictionary<string, Type> KnownFormatters { get; }
    /// <summary>
    /// List of all known locales.
    /// </summary>
    public List<string> KnownLocales { get; }

    // Dynamic configs.

    /// <summary>
    /// Returns current culture decided by the user.
    /// </summary>
    public string Locale { get; }
    
    /// <summary>
    /// Returns current selected command line formatter.
    /// </summary>
    public string Formatter { get; }

    /// <summary>
    /// Saves current config to storage.
    /// </summary>
    public void SaveConfig();

    /// <summary>
    /// Should save this config as is to storage.
    /// </summary>
    public void LoadConfig();

    /// <summary>
    /// Should delete config file.
    /// </summary>
    public void PurgeConfig();

    /// <summary>
    /// Should change the locale in the config.
    /// </summary>
    /// <param name="locale">New locale.</param>
    public void ChangeLocale(string locale);

    /// <summary>
    /// Should change the default console formatter.
    /// </summary>
    /// <param name="formatter">New formatter.</param>
    public void ChangeFormatter(string formatter);
}