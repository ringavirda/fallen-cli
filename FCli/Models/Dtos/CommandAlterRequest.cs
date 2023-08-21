// FCli namespaces.
using FCli.Models.Types;

namespace FCli.Models.Dtos;

/// <summary>
/// Necessary information for the command to be created or modified.
/// </summary>
public class CommandAlterRequest
{
    public string Name { get; set; } = string.Empty;
    public CommandType Type { get; set; } = CommandType.None;
    public ShellType Shell { get; set; } = ShellType.None;
    public string Path { get; set; } = string.Empty;
    public string Options { get; set; } = string.Empty;
}
