// Vendor namespaces.
using System.Text.Json.Serialization;

namespace FCli.Models;

/// <summary>
/// Abstraction for a sequence of commands.
/// </summary>
[JsonSerializable(typeof(Group))]
public class Group : Command
{
    /// <summary>
    /// Command designators, stored in an execution sequence.
    /// </summary>
    public List<string> Sequence { get; init;} = new();
}