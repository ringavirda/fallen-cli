// FCli namespaces.
using FCli.Common;
using FCli.Common.Exceptions;
using static FCli.Models.Args;

namespace FCli.Models;

/// <summary>
/// Base class for all tools and tool prototypes.
/// </summary>
/// <remarks>
/// Contains common properties and some guarding methods.
/// <remarks>
public abstract class Tool
{
    /// <summary>
    /// Toll's command line selector.
    /// </summary>
    public string Name { get; protected set; } = "default";
    /// <summary>
    /// Information that should be displayed with <c>--help</c> flag.
    /// </summary>
    public string Description { get; protected set; } = string.Empty;
    /// <summary>
    /// Known aliases for the selector of the tool.
    /// </summary>
    public List<string> Selectors { get; protected set; } = new();
    /// <summary>
    /// Unique descriptor for the tool.
    /// </summary>
    public ToolType Type { get; protected set; } = ToolType.None;
    /// <summary>
    /// Action that contains actual logic of the tool.
    /// </summary>
    /// <remarks>
    /// Accepts an arg and list of Flags.
    /// </remarks>
    public Action<string, List<Flag>> Action { get; protected set; } = null!;

    /// <summary>
    /// Asserts that given flag has no value.
    /// </summary>
    /// <param name="flag">Flag to test.</param>
    /// <exception cref="FlagException">If value is present.</exception>
    protected static void FlagHasNoValue(Flag flag, string toolName)
    {
        if (flag.Value != "")
        {
            Helpers.DisplayWarning(toolName, $"""
                Flag (--{flag.Key}) shouldn't have any value.
                To see list of all supported flags for {toolName} tool consult
                help page using --help flag.
                """);
            throw new FlagException($"--{flag.Key} - cannot have value.");
        }
    }

    /// <summary>
    /// Asserts that given flag has a value.
    /// </summary>
    /// <param name="flag">Flag to test.</param>
    /// <exception cref="FlagException">If value is missing.</exception>
    protected static void FlagHasValue(Flag flag, string toolName)
    {
        if (flag.Value == "")
        {
            Helpers.DisplayWarning(toolName, $"""
                Flag (--{flag.Key}) should have a value.
                To see list of all supported flags for {toolName} tool consult
                help page using --help flag.
                """);
            throw new FlagException($"--{flag.Key} - should have value.");
        }
    }

    /// <summary>
    /// Throws cause given flag is not known to the tool.
    /// </summary>
    /// <param name="flag">Unknown flag.</param>
    /// <param name="toolName">Tool name that doesn't recognize the flag.</param>
    /// <exception cref="FlagException">Flag is unknown.</exception>
    protected static void UnknownFlag(Flag flag, string toolName)
    {
        Helpers.DisplayWarning(toolName, $"""
            Flag (--{flag.Key}) is not a known flag for the {toolName} tool.
            To see list of all supported flags for {toolName} tool consult
            help page using --help flag.
            """);
        throw new FlagException(
             $"--{flag.Key} - is not a valid flag for {toolName} tool.");
    }

    /// <summary>
    /// Asserts that the given URL is valid.
    /// </summary>
    /// <param name="url">URL to test.</param>
    /// <returns>Constructed URI object.</returns>
    /// <exception cref="ArgumentException">If URI construction fails.</exception>
    protected static Uri ValidateUrl(string url, string toolName)
    {
        // Attempt create a URI from given url.
        var success = Uri.TryCreate(
            url,
            UriKind.Absolute,
            out Uri? uri)
            // Include http and https.
            && (uri?.Scheme == Uri.UriSchemeHttp
            || uri?.Scheme == Uri.UriSchemeHttps);
        // Guard against URI creation fail.
        if (!success || uri == null)
        {
            Helpers.DisplayWarning(toolName, $"{url} - is not a valid url.");
            throw new ArgumentException($"Given url ({url}) is invalid.");
        }
        // Return constructed URI.
        return uri;
    }

    /// <summary>
    /// Asserts that the given path valid and exists.
    /// </summary>
    /// <param name="path">Path to test.</param>
    /// <returns>Full path.</returns>
    /// <exception cref="ArgumentException">If path doesn't exist.</exception>
    protected static string ValidatePath(string path, string toolName)
    {
        // Guard against bad path.
        if (!File.Exists(path))
        {
            Helpers.DisplayWarning(toolName, $"{path} - is not a valid system path.");
            throw new ArgumentException($"Given path ({path}) is invalid.");
        }
        // Return path converting to full.
        return Path.GetFullPath(path);
    }
}
