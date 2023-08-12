// FCli namespaces.
using FCli.Models;
using FCli.Models.Types;

namespace FCli.Services.Abstractions;

/// <summary>
/// Describes class that parses command args and executes tools.
/// </summary>
public interface IToolExecutor
{
    /// <summary>
    /// Should execute tool of given type with given args.
    /// </summary>
    public void Execute(Args args, ToolType type);
    /// <summary>
    /// Should determine tool type form args or return None.
    /// </summary>
    public ToolType ParseType(Args args);
}
