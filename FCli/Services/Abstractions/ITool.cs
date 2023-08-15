// FCli namespaces.
using static FCli.Models.Args;

namespace FCli.Services.Abstractions;

/// <summary>
/// Represents a command line tool.
/// </summary>
public interface ITool : IToolDescriptor
{
    /// <summary>
    /// Performs main tool logic.
    /// </summary>
    /// <param name="arg">Tool's arg.</param>
    /// <param name="flags">Tool's flags</param>
    public void Execute(string arg, IEnumerable<Flag> flags);
}
