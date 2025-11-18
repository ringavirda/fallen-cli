using System.Text.Json;

using FCli.Exceptions;
using FCli.Models;
using FCli.Services.Abstractions;

namespace FCli.Services.Data;

/// <summary>
/// Command loader that uses json format to store and read commands.
/// </summary>
/// <remarks>
/// Storage file location is specified through the <c>DynamicConfig</c> properties.
/// </remarks>
public class JsonLoader : ICommandLoader
{
    // DI
    private readonly IConfig _config;

    // Loaded commands are buffered here to lover the amount of IO calls.
    private List<Command>? _commandCashe;

    public JsonLoader(IConfig config)
    {
        _config = config;

        // Guard against first launch.
        if (!Directory.Exists(_config.AppFolderPath))
            Directory.CreateDirectory(_config.AppFolderPath);
    }

    /// <summary>
    /// Attempts to load a command.
    /// </summary>
    /// <remarks>
    /// Checks buffer and attempts to load commands otherwise.
    /// </remarks>
    /// <param name="name">The name of the command.</param>
    /// <returns>Loaded command, or <c>null</c> if it wasn't found.</returns>
    public Command? LoadCommand(string name)
        => _commandCashe != null
            ? _commandCashe.FirstOrDefault(command => command.Name == name)
            : LoadCommands()?.FirstOrDefault(command => command.Name == name)
            ?? null;

    /// <summary>
    /// Checks if command exists.
    /// </summary>
    /// <remarks>
    /// Uses buffer and attempts to <c>LoadCommands</c> if it is empty.
    /// </remarks>
    /// <param name="name">The name of the command.</param>
    /// <returns><c>true</c> if command exists, <c>false</c> if not.</returns>
    public bool CommandExists(string name)
        => _commandCashe != null
            ? _commandCashe.Any(command => command.Name == name)
            : LoadCommands()?.Any(command => command.Name == name)
            ?? false;

    /// <summary>
    /// Loads all commands from storage file specified in <c>DynamicConfig</c>. 
    /// </summary>
    /// <remarks>
    /// <para>
    /// Populates commands buffer if those commands were successfully
    /// loaded and parsed.
    /// </para>
    /// <para>
    /// Uses System.Text.Json serializer to work with json string. 
    /// </para>
    /// </remarks>
    /// <returns>Loaded command buffer, or <c>null</c> if load fails.</returns>
    /// <exception cref="CriticalException">If command deserialization fails.</exception>
    public List<Command>? LoadCommands()
    {
        // Guard against empty storage.
        if (File.Exists(_config.StorageFilePath))
        {
            var json = File.ReadAllText(_config.StorageFilePath);
            // Guard against empty file.
            if (string.IsNullOrEmpty(json)) return null;
            // Attempt to deserialize commands form json string.
            try
            {
                var commands = JsonSerializer.Deserialize<List<Command>>(json);
                return _commandCashe = commands;

            }
            catch (JsonException ex)
            {
                // Since this is a critical failure, throw exception straight 
                // to the root selector.
                throw new CriticalException(
                    "[Loader] Commands weren't able to deserialize.", ex);
            }
        }
        else return null;
    }

    /// <summary>
    /// Saves given command to storage.
    /// </summary>
    /// <remarks>
    /// Refreshes the whole storage file upon saving the command.
    /// </remarks>
    /// <param name="command">Command object to save.</param>
    public void SaveCommand(Command command)
    {
        // Load command or use buffer if populated.
        var commands = _commandCashe ?? LoadCommands();
        // Guard against empty storage.
        if (commands == null)
            _commandCashe = [command];
        else commands.Add(command);
        // This method rewrites the whole command storage.
        RefreshStorage();
    }

    /// <summary>
    /// Attempts to delete given command.
    /// </summary>
    /// <remarks>
    /// Refreshes the whole storage file if deletion is successful.
    /// </remarks>
    /// <param name="name">The name of the command to delete.</param>
    /// <exception cref="ArgumentException">If command doesn't exist.</exception>
    public void DeleteCommand(string name)
    {
        // Attempt load command.
        var command = LoadCommand(name);
        // Guard against unknown command.
        if (command != null) _commandCashe?.Remove(command);
        else throw new ArgumentException(
            $"[Loader] Attempt to delete an unknown command ({name}).");
        // Rewrite storage without the deleted command.
        RefreshStorage();
    }

    /// <summary>
    /// Rewrites entire storage file with new commands.
    /// </summary>
    /// <remarks>
    /// Uses command buffer as a source.
    /// </remarks>
    private void RefreshStorage()
        => File.WriteAllText(
            _config.StorageFilePath,
            JsonSerializer.Serialize(_commandCashe));
}