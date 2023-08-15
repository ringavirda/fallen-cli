// FCli namespaces.
using FCli.Models;

namespace FCli.Services.Abstractions;

/// <summary>
/// Tries to parse array of command line args in the most appropriate way.
/// </summary>
public interface IArgsParser
{
    /// <summary>
    /// Should correctly parse raw command line args into fcli specific args
    /// abstraction object.
    /// </summary>
    /// <param name="args">Command line args.</param>
    /// <returns>Parsed Args object.</returns>
    public Args ParseArgs(string[] args);
}
