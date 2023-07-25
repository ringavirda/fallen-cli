// FCli namespaces.
using FCli.Models;

namespace FCli.Services;

/// <summary>
/// Describes class that parses command args and executes tools.
/// </summary>
public interface IToolExecutor
{
    /// <summary>
    /// List of all known flags that describe command flavors.
    /// </summary>
    public List<string> KnownTypeFlags { get; }
    /// <summary>
    /// List of all tool prototypes.
    /// </summary>
    public List<Tool> KnownTools { get; }
    /// <summary>
    /// Should execute tool of given type with given args.
    /// </summary>
    public void Execute(Args args, ToolType type);
    /// <summary>
    /// Should determine tool type form args or return None.
    /// </summary>
    public ToolType ParseType(Args args);
}
