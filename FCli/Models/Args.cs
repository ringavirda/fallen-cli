// Vendor namespaces.
using System.Text.RegularExpressions;
// FCli namespaces.
using FCli.Services.Format;

namespace FCli.Models;

/// <summary>
/// Wrapper class for the command line arguments.
/// </summary>
/// <remarks>
/// Parses arguments pretty reliably (not always).
/// </remarks>
public partial class Args
{
    // Singleton none Args.
    private static readonly Args _none = new("", "", new());
    // Regex needed to parse quoted strings.
    // Because of this Args class is partial.
    [GeneratedRegex("[\",'].*?[\",']")]
    private static partial Regex WithinQuotes();
    // Regex to determine if an arg is a path.
    [GeneratedRegex(@"^[.,/,\\].")]
    private static partial Regex IsPath();

    /// <summary>
    /// Constructor with all command or tool parameters.
    /// </summary>
    /// <remarks>
    /// It is privated cause <c>Args</c> are meant to be parsed.
    /// </remarks>
    /// <param name="selector">The name of the command or tool.</param>
    /// <param name="arg">Argument for the tool.</param>
    /// <param name="flags">Given command line flags.</param>
    private Args(string selector, string arg, List<Flag> flags)
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
    public string Selector { get; init; }
    /// <summary>
    /// Argument (usually path) for the tool.
    /// </summary>
    /// <remarks>
    /// Commands use <c>--options</c> flag.
    /// </remarks>
    public string Arg { get; init; }
    /// <summary>
    /// List of all parsed flags.
    /// </summary>
    /// <remarks>
    /// <c>--</c> starter is dropped.
    /// </remarks>
    public List<Flag> Flags { get; init; }

    /// <summary>
    /// Attempts to parse given command line args.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Splits args regardless of their organization in the <c>args</c> array.
    /// </para>
    /// <para>
    /// Recognizes strings wrapped in <c>""</c> or <c>''</c> as one argument.
    /// </para>
    /// <para>
    /// Each string starting with <c>--</c> is treated as a frag key.
    /// </para>
    /// <para>
    /// One string after a flag key without a starter is treated as a frag argument.
    /// </para>
    /// </remarks>
    /// <param name="args">Array of command line arguments.</param>
    /// <returns>Parsed <c>Args</c> object.</returns>
    /// <exception cref="ArgumentException">If finds more then one arg and selector.</exception>
    public static Args Parse(string[] args)
    {
        // Logic of splitting the args is forwarded to separate method.
        args = SplitArgs(args);
        // Buffer is needed to control the parsing process.
        var buffer = args.ToList();
        // Forward creating of the Flags list.
        var flags = new List<Flag>();
        // Guard against empty args.
        if (args.Length == 0) return None;
        else
        {
            // This loop parses flags and removes them from the buffer.
            for (int i = 0; i < args.Length; i++)
            {
                // Find flag key.
                if (args[i].StartsWith("--"))
                {
                    // Check if flag has an argument and create Flag.
                    var flag =
                        i < args.Length - 1 && !args[i + 1].StartsWith("--")
                        ? new Flag(args[i][2..^0], args[i + 1])
                        : new Flag(args[i][2..^0], "");
                    // Add flag to the list.
                    flags.Add(flag);
                    // Remove flag key from the buffer.
                    buffer.Remove(args[i]);
                    // Remove flag argument from the buffer if present.
                    if (flag.Value != "")
                        buffer.Remove(flag.Value);
                }
            }
            // Guard against inappropriate count of args.
            if (buffer.Count > 2)
            {
                // Hardcode inline formatter.
                new InlineFormatter().DisplayWarning(
                    nameof(Args),
                    "FCli accepts only <tool?> <arg>, but more then one arg was found.");
                throw new ArgumentException(
                    $"Incorrect ({buffer.Count}) amount of args was given.");
            }
            // Construct and return Args object.
            return new Args(
                buffer.Count >= 1 ? buffer[0] : "",
                buffer.Count == 2 ? buffer[1] : "",
                flags);
        }
    }

    /// <summary>
    /// Points to empty Args object.
    /// </summary>
    public static Args None => _none;

    /// <summary>
    /// Parses array of strings and splits them accordingly (hopefully).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Possibility of all args being  in one string is considered.
    /// </para>
    /// <para>
    /// Strings surrounded in quotes are supported.
    /// </para>
    /// </remarks>
    /// <param name="args">Command line args to split.</param>
    /// <returns>Command line args correctly split.</returns>
    private static string[] SplitArgs(string[] args)
    {
        // Buffet for construction of the new args. 
        var newArgs = new List<string>();
        // Main cycle.
        foreach (var arg in args)
        {
            // Match is a string surrounded with quotes if such is present.
            var match = WithinQuotes().Match(arg);
            // Guard against empty match.
            if (match.Success)
            {
                // Assumes that only one quoted string (path) is present.
                // Removes matched string from the original string.
                // Splits the rest of args and adds them to newArgs.
                newArgs.AddRange(
                    arg.Remove(match.Index, match.Length).Split(" "));
                // Adds matched quoted string, trimming the quotes.
                newArgs.Add(match.Value.Trim(match.Value[0]));
            }
            // Add path arguments untouched.
            else if (IsPath().Match(arg).Success)
                newArgs.Add(arg);
            // Add splitted args.
            else newArgs.AddRange(arg.Split(" "));
        }
        // Filter all empty strings and return reconstructed args.
        return newArgs.Where(s => s != string.Empty).ToArray();
    }
}
