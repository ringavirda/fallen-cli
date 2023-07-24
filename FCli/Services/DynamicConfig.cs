namespace FCli.Services;

/// <summary>
/// Static service for determining dynamic configs for the fallen-cli.
/// </summary>
/// <remarks>
/// Changes configuration according to user's operating system.
/// </remarks>
public class DynamicConfig
{
    /// <summary>
    /// Name for the root app folder.
    /// </summary>
    public virtual string AppFolderName { get; private set; }
    /// <summary>
    /// Root app path.
    /// </summary>
    public virtual string AppFolderPath { get; private set; }
    /// <summary>
    /// Name for the command storage file.
    /// </summary>
    public virtual string StorageFileName { get; private set; }
    /// <summary>
    /// Path to the command storage file.
    /// </summary>
    public virtual string StorageFilePath { get; private set; }
    /// <summary>
    /// Template for log file names.
    /// </summary>
    public virtual string LogsFileTemplate { get; private set; }
    /// <summary>
    /// Name for the folder that contains logs.
    /// </summary>
    public virtual string LogsFolderName { get; private set; }
    /// <summary>
    /// Path to the logs template.
    /// </summary>
    public virtual string LogsPath { get; private set; }

#pragma warning disable 8618
    public DynamicConfig()
    {
        if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            ConfigureWindows();
        else if (Environment.OSVersion.Platform == PlatformID.Unix)
            ConfigureUnix();
        else throw new PlatformNotSupportedException(
            "FCli supports only WinNT and Unix based systems.");
    }
#pragma warning restore 8618

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

        LogsFolderName = "logs";
        LogsFileTemplate = "fcli-log.log";
        LogsPath = Path.Combine(
            AppFolderPath,
            LogsFolderName,
            LogsFileTemplate);
    }
}