// FCli namespaces.
using FCli.Models.Types;

namespace FCli.Services.Abstractions;

/// <summary>
/// Represents main tool info.
/// </summary>
public interface IToolDescriptor
{
    /// <summary>
    /// Toll's command line selector.
    /// </summary>
    public abstract string Name { get; }
    /// <summary>
    /// Information that should be displayed with <c>help</c> flag.
    /// </summary>
    public abstract string Description { get; }
    /// <summary>
    /// Known aliases for the selector of the tool.
    /// </summary>
    public List<string> Selectors { get; }
    /// <summary>
    /// Unique descriptor for the tool.
    /// </summary>
    public ToolType Type { get; }
}