using FCli.Models;

namespace FCli.Services.Abstractions;

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