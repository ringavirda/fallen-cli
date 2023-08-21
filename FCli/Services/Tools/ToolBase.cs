// FCli namespaces.
using System.Net.Mail;
using FCli.Exceptions;
using FCli.Models.Types;
using FCli.Services.Abstractions;
using static FCli.Models.Args;

namespace FCli.Services.Tools;

/// <summary>
/// Base class for all known tools.
/// </summary>
/// <remarks>
/// Contains common properties and some guarding methods.
/// <remarks>
public abstract class ToolBase : ITool
{
    // DI needed by all tools.
    protected readonly ICommandLineFormatter _formatter;
    protected readonly IResources _resources;

    /// <summary>
    /// Default constructor for the descriptors.
    /// </summary>
    public ToolBase()
        : this(null!, null!) { }

    protected ToolBase(
        ICommandLineFormatter formatter,
        IResources resources)
    {
        _formatter = formatter;
        _resources = resources;
    }

    // From ITool interface.

    // Pass abstractions down the hierarchy.
    public abstract string Name { get; }
    public abstract string Description { get; }
    public abstract List<string> Selectors { get; }
    public abstract ToolType Type { get; }

    /// <summary>
    /// List of parsed flags.
    /// </summary>
    public List<Flag> Flags { get; private set; } = null!;
    /// <summary>
    /// Initialized by the Execute method.
    /// </summary>
    public string Arg { get; protected set; } = null!;

    /// <summary>
    /// Performs tool's general logic of processing flags and acting.
    /// </summary>
    /// <param name="arg">Tool's arg.</param>
    /// <param name="flags">Tool's flags.</param>
    public void Execute(string arg, IEnumerable<Flag> flags)
    {
        // Init props.
        Arg = arg;
        Flags = flags.ToList();

        // Handle --help flag.
        if (flags.Any(flag => flag.Key == "help"))
        {
            _formatter.DisplayMessage(Description);
            return;
        }

        // Perform general validation and initialization.
        GuardInit();


        // Process flags.
        foreach (var flag in Flags)
            ProcessNextFlag(flag);

        // Perform action.
        try
        {
            ActionAsync().Wait();
        }
        // Guard against internal cancels.
        catch (AggregateException) { }
    }

    // Abstract methods.

    /// <summary>
    /// Performs necessary general validation over received arg and flags.
    /// </summary>
    /// <remarks>
    /// Can initialize some private values.
    /// </remarks>
    protected abstract void GuardInit();

    /// <summary>
    /// Receives each flag sequentially and need to process them accordingly.
    /// </summary>
    /// <param name="flag">Next flag to be processed.</param>
    protected abstract void ProcessNextFlag(Flag flag);

    /// <summary>
    /// Main tool logic, performed after all flags were processed.
    /// </summary>
    protected abstract Task ActionAsync();

    // Protected validators.

    /// <summary>
    /// Asserts that given flag has no value.
    /// </summary>
    /// <param name="flag">Flag to test.</param>
    /// <exception cref="FlagException">If value is present.</exception>
    protected void FlagHasNoValue(Flag flag, string toolName)
    {
        if (flag.Value != "")
        {
            _formatter.DisplayError(
                toolName,
                string.Format(
                    _resources.GetLocalizedString("Tool_FlagShouldNotHaveValue"),
                    flag.Key, 
                    toolName));
            throw new FlagException(
                $"[{toolName}] --{flag.Key} - cannot have value.");
        }
    }

    /// <summary>
    /// Asserts that given flag has a value.
    /// </summary>
    /// <param name="flag">Flag to test.</param>
    /// <exception cref="FlagException">If value is missing.</exception>
    protected void FlagHasValue(Flag flag, string toolName)
    {
        if (flag.Value == "")
        {
            _formatter.DisplayError(
                toolName,
                string.Format(
                    _resources.GetLocalizedString("Tool_FlagShouldHaveValue"),
                    flag.Key,
                    toolName));
            throw new FlagException(
                $"[{toolName}] --{flag.Key} - should have value.");
        }
    }

    /// <summary>
    /// Throws cause given flag is not known to the tool.
    /// </summary>
    /// <param name="flag">Unknown flag.</param>
    /// <param name="toolName">Tool name that doesn't recognize the flag.</param>
    /// <exception cref="FlagException">Flag is unknown.</exception>
    protected void UnknownFlag(Flag flag, string toolName)
    {
        _formatter.DisplayError(
                toolName,
                string.Format(
                    _resources.GetLocalizedString("Tool_FlagIsUnknown"),
                    flag.Key,
                    toolName,
                    toolName));
        throw new FlagException(
             $"[{toolName}] --{flag.Key} - is not a valid flag.");
    }

    /// <summary>
    /// Asserts that the given URL is valid.
    /// </summary>
    /// <param name="url">URL to test.</param>
    /// <returns>Constructed URI object.</returns>
    /// <exception cref="ArgumentException">If URI construction fails.</exception>
    protected Uri ValidateUrl(string url, string toolName)
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
            _formatter.DisplayError(
                toolName,
                string.Format(
                    _resources.GetLocalizedString("Tool_UrlIsInvalid"),
                    url));
            throw new ArgumentException(
                $"[{toolName}] Given url ({url}) is invalid.");
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
    protected string ValidatePath(string path, string toolName)
    {
        // Guard against bad path.
        if (!(File.Exists(path) || Directory.Exists(path)))
        {
            _formatter.DisplayError(
                toolName,
                string.Format(_resources.GetLocalizedString("Tool_PathIsInvalid"),
                    path));
            throw new ArgumentException(
                $"[{toolName}] Given path ({path}) is invalid.");
        }
        // Return path converting to full.
        return Path.GetFullPath(path);
    }

    /// <summary>
    /// Validates the format of the given email string.
    /// </summary>
    /// <param name="email">Email to check.</param>
    /// <param name="toolName">Sender.</param>
    /// <returns>Parsed email string.</returns>
    /// <exception cref="ArgumentException">If assertion fails.</exception>
    protected string ValidateEmail(string email, string toolName)
    {
        if (!MailAddress.TryCreate(email, out var address))
        {
            _formatter.DisplayError(
                toolName,
                string.Format(
                    _resources.GetLocalizedString("Tool_EmailIsInvalid"),
                    email));
            throw new ArgumentException(
                $"[{toolName}] Given email ({email}) is invalid.");
        }
        else return address.Address;
    }

    /// <summary>
    /// Generic user confirmation for an action.
    /// </summary>
    /// <returns>True if confirmed.</returns>
    protected bool UserConfirm()
    {
        _formatter.DisplayMessage(
            _resources.GetLocalizedString("FCli_Confirm"));
        var confirm = _formatter.ReadUserInput("(yes/any)");
        if (confirm != "yes")
        {
            _formatter.DisplayMessage(
                _resources.GetLocalizedString("FCli_Averted"));
            return false;
        }
        else
        {
            _formatter.DisplayMessage(
                _resources.GetLocalizedString("FCli_Continued"));
            return true;
        }
    }
}
