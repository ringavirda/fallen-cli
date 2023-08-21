// Vendor namespaces.
using System.Text.RegularExpressions;
// FCli namespaces.
using FCli.Models;
using FCli.Services.Abstractions;

namespace FCli.Services;

/// <summary>
/// Transforms array of args to <c>Args</c> object.
/// </summary>
/// <remarks>
/// Parses command line args differently if they are flat.
/// </remarks>
public partial class ArgsParser : Args, IArgsParser
{
    // Regex needed to parse quoted strings.
    // Because of this Args class is partial.
    [GeneratedRegex("[\",'].*?[\",']")]
    private static partial Regex WithinQuotes();
    // Regex to determine if an arg is a path.
    [GeneratedRegex(@"^[.,/,\\].")]
    private static partial Regex IsPath();

    // DI.
    private readonly ICommandLineFormatter _formatter;
    private readonly IResources _resources;

    public ArgsParser(ICommandLineFormatter formatter, IResources resources)
        : base()
    {
        _formatter = formatter;
        _resources = resources;
    }

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
    public Args ParseArgs(string[] args)
    {
        // Logic of splitting the args is forwarded to separate method.
        if (args.Length == 1)
            args = SplitArgsOneLine(args);
        // Buffer is needed to control the parsing process.
        var buffer = args.ToList();
        // Guard against empty args.
        if (args.Length == 0) return None;
        else
        {
            // This loop parses flags and removes them from the buffer.
            for (int i = 0; i < args.Length; i++)
            {
                // Find flag key.
                if (args[i].StartsWith("--", StringComparison.CurrentCulture))
                {
                    // Check if flag has an argument and create Flag.
                    var flag =
                        i < args.Length - 1 && !args[i + 1]
                            .StartsWith("--", StringComparison.CurrentCulture)
                        ? new Flag(args[i][2..^0], args[i + 1])
                        : new Flag(args[i][2..^0], "");
                    // Add flag to the list.
                    Flags.Add(flag);
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
                _formatter.DisplayError(
                    "Args",
                    _resources.GetLocalizedString("FCli_MultipleArgs"));
                throw new ArgumentException(
                    $"[Arg] Incorrect ({buffer.Count}) amount of args was given.");
            }
            // Construct and return Args from this object.
            Selector = buffer.Count >= 1 ? buffer[0] : string.Empty;
            Arg = buffer.Count == 2 ? buffer[1] : string.Empty;
            return this;
        }
    }

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
    private static string[] SplitArgsOneLine(string[] args)
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
