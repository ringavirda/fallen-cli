using FCli.Exceptions;
using FCli.Models;
using FCli.Models.Types;
using FCli.Services.Abstractions;

using Microsoft.Extensions.Logging;

namespace FCli.Services;

/// <summary>
/// Generic implementation of ToolExecutor.
/// </summary>
public class ToolExecutor(
    ILogger<ToolExecutor> logger,
    IEnumerable<ITool> tools) : IToolExecutor
{
    // DI.
    private readonly ILogger<ToolExecutor> _logger = logger;
    private readonly IEnumerable<ITool> _tools = tools;

    // Logging.
    private static readonly Action<ILogger, string, Exception> LogArgument
        = LoggerMessage.Define<string>(
            LogLevel.Information,
            3,
            "Argument or flag failed: {Message}");
    private static readonly Action<ILogger, string, Exception> LogIdentity
        = LoggerMessage.Define<string>(
            LogLevel.Warning,
            4,
            "Operation involving identities failed: {Message}");

    /// <summary>
    /// Execute tool from given type and arg.
    /// </summary>
    /// <param name="args">Tool argument.</param>
    /// <param name="type">Tool type to execute.</param>
    /// <exception cref="CriticalException">If tool selection fails.</exception>
    public void Execute(Args args, ToolType type)
    {
        // Extract tool from the list of known tools.
        var tool = _tools.FirstOrDefault(tool => tool.Type == type)
            ?? throw new CriticalException("[Tool] Tool wasn't extracted.");
        // Perform action.
        try
        {
            tool.Execute(args.Arg, args.Flags);
        }
        catch (ArgumentException ex)
        {
            // Flag and Arg exceptions are caused by user errors and so have
            // low priority for logging.
            LogArgument(
                _logger,
                "[Tool] Argument or flags has failed.",
                ex);
        }
        catch (IdentityException ex)
        {
            // Identity exceptions are considered to have higher priority than
            // argument exceptions, hence warning level.
            LogIdentity(
                _logger,
                "[Tool] Identity failed.",
                ex);
        }
    }

    /// <summary>
    /// Parses given args and returns tool type if recognized.
    /// </summary>
    /// <param name="args">Args to analyze.</param>
    /// <returns>Tool type or None.</returns>
    public ToolType ParseType(Args args)
    {
        // Guard against empty arg.
        if (args.Selector == "") return ToolType.None;
        // Parse selector.
        var selector = args.Selector;
        foreach (var tool in _tools)
        {
            if (tool.Selectors.Contains(selector))
                return tool.Type;
        }
        // Return None if no match found.
        return ToolType.None;
    }
}