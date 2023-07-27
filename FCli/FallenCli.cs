// Vendor namespaces.
using Microsoft.Extensions.Logging;
using System.Resources;
// FCli namespaces.
using FCli.Models;
using FCli.Models.Types;
using FCli.Exceptions;
using FCli.Services;
using FCli.Services.Format;

namespace FCli;

/// <summary>
/// Fallen-cli facade class.
/// </summary>
/// <remarks>
/// Implemented as background service for the app hosting container.
/// </remarks>
public class FallenCli
{
    // DI.
    private readonly IToolExecutor _executor;
    private readonly ICommandFactory _factory;
    private readonly ILogger<FallenCli> _logger;
    private readonly ICommandLineFormatter _formatter;
    private readonly ResourceManager _resources;

    public FallenCli(
        IToolExecutor toolExecutor,
        ICommandFactory commandFactory,
        ILogger<FallenCli> logger,
        ICommandLineFormatter formatter,
        ResourceManager resources)
    {
        _executor = toolExecutor;
        _factory = commandFactory;
        _logger = logger;
        _formatter = formatter;
        _resources = resources;
    }

    /// <summary>
    /// Executes main fcli logic.
    /// </summary>
    public void Execute(string[] cArgs)
    {
        try
        {
            // Parse command line args into Args object.
            var args = Args.Parse(cArgs);
            // Handle no args.
            if (args == Args.None)
            {
                _formatter.EchoGreeting();
                return;
            }
            // Handle --help flag.
            else if (args.Flags.Any(flag => flag.Key == "help")
                && args.Selector == string.Empty)
            {
                _formatter.EchoHelp();
                return;
            }
            // Handle --version flag.
            else if (args.Flags.Any(flag => flag.Key == "version"))
            {
                _formatter.EchoNameAndVersion();
                return;
            }
            // Try parse tool type.
            var toolType = _executor.ParseType(args);
            // If failed consider it as a command.
            if (toolType == ToolType.None)
            {
                try
                {
                    var command = _factory.Construct(args.Selector);
                    // Guard against what should never happen.
                    if (command.Action == null)
                        throw new CriticalException(
                            "Command wasn't constructed properly!");
                    else command.Action();
                }
                catch (InvalidOperationException ex)
                {
                    _logger.LogWarning(
                        ex,
                        "User tried to invoke unsupported command.");
                    return;
                }
            }
            // Execute tool otherwise.
            else _executor.Execute(args, toolType);
            // Stop app process regardless.
            return;
        }
        // Root exception selector.
        catch (Exception ex)
        {
            _formatter.DisplayError(
                "FCli", 
                string.Format(
                    _resources.GetString("FCli_CriticalError")
                    ?? _formatter.StringNotLoaded(),
                    ex.GetType().Name, ex.Message
                ));
            _logger.LogCritical(ex, "An unexpected or critical exception was thrown.");
            return;
        }
    }
}
