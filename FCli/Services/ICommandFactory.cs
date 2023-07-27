// FCli namespaces.
using FCli.Models;
using FCli.Models.Types;

namespace FCli.Services;

/// <summary>
/// Describes generic factory that constructs commands from templates.
/// </summary>
public interface ICommandFactory
{
    /// <summary>
    /// Should load a command template from the storage and construct it.
    /// </summary>
    public Command Construct(string name);
    /// <summary>
    /// Should construct new command from the given template.
    /// </summary>
    public Command Construct(
        string name,
        string path,
        CommandType type,
        ShellType shell,
        string options);

    /// <summary>
    /// Should generate a group of commands.
    /// </summary>
    public Group ConstructGroup(string name, List<string> commands);
}
