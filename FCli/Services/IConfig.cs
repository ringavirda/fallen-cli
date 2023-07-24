namespace FCli.Services;

public interface IConfig
{
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
}