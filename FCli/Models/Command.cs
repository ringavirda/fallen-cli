// Vendor namespaces.
using System.Text.Json.Serialization;
using FCli.Models.Dtos;
// FCli namespaces.
using FCli.Models.Types;

namespace FCli.Models;

/// <summary>
/// Abstraction for a recorded command.
/// </summary>
/// <remarks>
/// All properties are mandatory except for Action, which can be added afterwards.
/// </remarks>
[JsonDerivedType(typeof(Group), typeDiscriminator: "group")]
[JsonDerivedType(typeof(Command), typeDiscriminator: "command")]
public class Command
{
    /// <summary>
    /// Command line selector of the command.
    /// </summary>
    public string Name { get; init; } = "default";
    /// <summary>
    /// Describes the way of execution.
    /// </summary>
    public CommandType Type { get; init; } = CommandType.None;
    /// <summary>
    /// Specifies shell type if this is a shell command.
    /// </summary>
    public ShellType Shell { get; init; } = ShellType.None;
    /// <summary>
    /// Path or URL to the resource.
    /// </summary>
    public string Path { get; init; } = string.Empty;
    /// <summary>
    /// Additional command line arguments if needed.
    /// </summary>
    public string Options { get; init; } = string.Empty;

    /// <summary>
    /// Action contains the actual logic for command execution.
    /// </summary>
    /// <remarks>
    /// Nullable to support Factory pattern and deserialize more cleanly.
    /// </remarks>
    [JsonIgnore]
    public Action? Action { get; set; }

    /// <summary>
    /// Transforms this command into an alteration request.
    /// </summary>
    /// <returns>AlterRequest for this command.</returns>
    public CommandAlterRequest ToAlterRequest()
        => new()
        {
            Name = Name,
            Path = Path,
            Type = Type,
            Shell = Shell,
            Options = Options,
        };
}
