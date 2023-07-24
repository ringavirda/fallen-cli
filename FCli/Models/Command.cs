// Vendor namespaces.
using System.Text.Json.Serialization;

namespace FCli.Models;

/// <summary>
/// Abstraction for a recorded command.
/// </summary>
/// <remarks>
/// All properties are mandatory except for Action, which can be added afterwards.
/// </remarks>
[JsonSerializable(typeof(Command))]
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
}
