namespace FCli.Models;

/// <summary>
/// Wrapper class for the command line arguments.
/// </summary>
/// <remarks>
/// Parses arguments pretty reliably (not always).
/// </remarks>
public class Args
{
    // Singleton none Args.
    private static readonly Args _none = new("", "", new());

    /// <summary>
    /// Protected default constructor.
    /// </summary>
    protected Args() { }

    /// <summary>
    /// Constructor with all command or tool parameters.
    /// </summary>
    /// <remarks>
    /// It is privated cause <c>Args</c> are meant to be parsed.
    /// </remarks>
    /// <param name="selector">The name of the command or tool.</param>
    /// <param name="arg">Argument for the tool.</param>
    /// <param name="flags">Given command line flags.</param>
    protected Args(string selector, string arg, List<Flag> flags)
    {
        Selector = selector;
        Arg = arg;
        Flags = flags;
    }

    /// <summary>
    /// Key-Value record that represents command line flag.
    /// </summary>
    /// <param name="Key">Flag selector.</param>
    /// <param name="Value">Flag argument.</param>
    public record Flag(
        string Key,
        string Value
    );

    /// <summary>
    /// Command line identifier for the command or tool.
    /// </summary>
    public string Selector { get; protected set; } = string.Empty;
    /// <summary>
    /// Argument (usually path) for the tool.
    /// </summary>
    /// <remarks>
    /// Commands use <c>--options</c> flag.
    /// </remarks>
    public string Arg { get; protected set; } = string.Empty;
    /// <summary>
    /// List of all parsed flags.
    /// </summary>
    /// <remarks>
    /// <c>--</c> starter is dropped.
    /// </remarks>
    public List<Flag> Flags { get; } = new();

    /// <summary>
    /// Points to empty Args object.
    /// </summary>
    public static Args None => _none;
}
