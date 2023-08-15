// FCli namespaces.
using FCli.Models;

namespace FCli.Services.Abstractions;

/// <summary>
/// Describes generic class that performs CRUD operations on <c>Command</c>.
/// </summary>
public interface ICommandLoader
{
    /// <summary>
    /// Should attempt loading a command from the given name and return
    /// <c>null</c> if fails.
    /// </summary>
    public Command? LoadCommand(string name);
    
    /// <summary>
    /// Should attempt loading the whole command storage and return <c>null</c>
    /// if the storage is empty or doesn't exist yet.
    /// </summary>
    public List<Command>? LoadCommands();
    
    /// <summary>
    /// Should save the given command to storage. 
    /// </summary>
    public void SaveCommand(Command command);
    
    /// <summary>
    /// Should check if command is present in the storage and return
    /// <c>false</c> if not.
    /// </summary>
    public bool CommandExists(string name);
    
    /// <summary>
    /// Should attempt to delete a command from the given name and throw if
    /// that command doesn't exist.
    /// </summary>
    public void DeleteCommand(string name);
}
