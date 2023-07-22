using System.Text.Json.Serialization;

namespace FCli.Models;

[JsonSerializable(typeof(Command))]
public class Command
{
    public string Name { get; init; } = "default"!;
    public CommandType Type { get; init; } = CommandType.None;
    public string Path { get; init; } = string.Empty;
    public string Options { get; init; } = string.Empty;

    [JsonIgnore]
    public Action Action { get; init; } = () => { };

}
