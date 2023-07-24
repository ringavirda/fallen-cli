namespace FCli.Models.Tools;

/// <summary>
/// Describes a generic tool prototype.
/// </summary>
public interface IToolProto
{
    /// <summary>
    /// Should return constructed tool.
    /// </summary>
    public Tool GetTool();
}
