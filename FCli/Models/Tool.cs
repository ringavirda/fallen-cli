using FCli.Common.Exceptions;

namespace FCli.Models;

public abstract class Tool
{
    public string Name { get; protected set; } = "default";
    public string Description { get; protected set; } = string.Empty;
    public List<string> Selectors { get; protected set; } = new();

    public ToolType Type { get; protected set; } = ToolType.None;
    public Action<string, List<Flag>> Action { get; protected set; } = null!;

    protected static void FlagHasNoValue(Flag flag)
    {
        if (flag.Value != "")
            throw new FlagException($"--{flag.Key} - cannot have value.");
    }

    protected static void FlagHasValue(Flag flag)
    {
        if (flag.Value == "")
            throw new FlagException($"--{flag.Key} - should have value.");
    }

    protected static void UnknownFlag(Flag flag, string toolName)
    {
        throw new FlagException(
             $"--{flag.Key} - is not a valid flag for {toolName} tool.");
    }

    protected static Uri ValidateUrl(string url)
    {
        var success = Uri.TryCreate(
            url,
            UriKind.Absolute,
            out Uri? uri)
            && (uri?.Scheme == Uri.UriSchemeHttp
            || uri?.Scheme == Uri.UriSchemeHttps);

        if (!success || uri == null)
            throw new ArgumentException("Given url is invalid.");

        return uri;
    }

    protected static string ValidatePath(string path)
    {
        if (!File.Exists(path))
            throw new ArgumentException("Given path is invalid.");
        return Path.GetFullPath(path);
    }
}