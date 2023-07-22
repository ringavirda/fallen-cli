using System.Text.Json;
using Microsoft.Extensions.Configuration;

using FCli.Models;

namespace FCli.Services.Data;

public class JsonLoader : ICommandLoader
{
    private readonly string _saveFileName = "storage.json";

    private readonly string _fullSavePath;
    private List<Command>? _loadedCommands;


    public JsonLoader(DynamicConfig dynamicConfig, IConfiguration configuration)
    {
        var saveLocation = dynamicConfig.StorageLocation;
        var saveFolderName = configuration.GetSection("Storage")
            .GetSection("StorageFolderName").Value ?? dynamicConfig.DefaultStorageFolderName;
        _fullSavePath = Path.Combine(
            saveLocation,
            saveFolderName,
            _saveFileName);

        if (!Directory.Exists(saveLocation))
            Directory.CreateDirectory(saveLocation);
        if (!Directory.Exists(Path.Combine(saveLocation, saveFolderName)))
            Directory.CreateDirectory(Path.Combine(saveLocation, saveFolderName));
    }

    public Command LoadCommand(string name)
    {
        if (CommandExists(name))
        {
            var command = _loadedCommands?
                .FirstOrDefault(command => command.Name == name);
            return command ?? throw new ArgumentException(
                $"{name} - is not a known command.");
        }
        else
            throw new ArgumentException($"{name} - is not a known command.");
    }

    public List<Command> LoadCommands()
    {
        if (File.Exists(_fullSavePath))
        {
            var json = File.ReadAllText(_fullSavePath);
            if (json == string.Empty)
                return new();
            var commands = JsonSerializer.Deserialize<List<Command>>(json);

            if (commands != null)
                return _loadedCommands = commands;
            else
                throw new InvalidOperationException(
                    "Commands wasn't able to deserialise.");
        }
        else
            return new();
    }

    public void SaveCommand(Command command)
    {
        string json;

        var commands = _loadedCommands ??
            (File.Exists(_fullSavePath) ? LoadCommands() : null);

        if (commands == null)
            json = JsonSerializer.Serialize(new List<Command> { command });
        else
        {
            commands.Add(command);
            json = JsonSerializer.Serialize(commands);
        }

        File.WriteAllText(_fullSavePath, json);
    }
    public bool CommandExists(string name)
    {
        return _loadedCommands != null
            ? _loadedCommands.Any(command => command.Name == name)
            : LoadCommands().Any(command => command.Name == name);
    }

    public void DeleteCommand(string name)
    {
        var command = LoadCommand(name);
        _loadedCommands?.Remove(command);

        var json = JsonSerializer.Serialize(_loadedCommands);
        File.WriteAllText(_fullSavePath, json);
    }
}
