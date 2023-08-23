namespace FCli.Models;

/// <summary>
/// Wrapper class for the command line arguments.
/// </summary>
/// <remarks>
/// Parses arguments pretty reliably (not always).
/// </remarks>
public class Args
{
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
    /// Command line identifier for the command or tool.
    /// </summary>
    public string Selector { get; protected set; } = string.Empty;
    /// <summary>
    /// Argument (usually path) for the tool.
    /// </summary>
    /// <remarks>
    /// Commands use [--options] flag.
    /// </remarks>
    public string Arg { get; protected set; } = string.Empty;
    /// <summary>
    /// List of all parsed flags.
    /// </summary>
    /// <remarks>
    /// [--] starter is dropped.
    /// </remarks>
    public List<Flag> Flags { get; } = new();
    /// <summary>
    /// Points to the empty <c>Args</c> object.
    /// </summary>
    public static Args None { get; } = new("", "", new());
}