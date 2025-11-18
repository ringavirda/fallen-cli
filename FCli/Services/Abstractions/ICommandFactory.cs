using FCli.Models;
using FCli.Models.Dtos;

namespace FCli.Services.Abstractions;

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
    public Command Construct(CommandAlterRequest request);

    /// <summary>
    /// Should generate a group of commands.
    /// </summary>
    public Group ConstructGroup(GroupAlterRequest request);
}