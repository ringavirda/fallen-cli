namespace FCli.Models.Dtos;

/// <summary>
/// Contains necessary information for creation or overriding group commands.
/// </summary>
public class GroupAlterRequest
{
    public string Name { get; set; } = string.Empty;
    public List<string> Sequence { get; set; } = new();
}
