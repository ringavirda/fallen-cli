using System.Globalization;

using FCli.Exceptions;
using FCli.Models;
using FCli.Models.Types;
using FCli.Services.Abstractions;

using Microsoft.Extensions.Logging;

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
    private readonly ICommandLineFormatter _formatter;
    private readonly IResources _resources;
    private readonly ILogger<FallenCli> _logger;
    private readonly IArgsParser _args;
    private readonly IToolExecutor _executor;
    private readonly ICommandFactory _factory;

    public FallenCli(
        ICommandLineFormatter formatter,
        IResources resources,
        ILogger<FallenCli> logger,
        IArgsParser args,
        IToolExecutor toolExecutor,
        ICommandFactory commandFactory)
    {
        _formatter = formatter;
        _resources = resources;
        _logger = logger;
        _args = args;
        _executor = toolExecutor;
        _factory = commandFactory;
    }

    // Logging.
    private static readonly Action<ILogger, string, Exception> InvalidOperation
        = LoggerMessage.Define<string>(
            LogLevel.Warning,
            2,
            "Invalid operation: {Message}");
    private static readonly Action<ILogger, string, Exception> Critical
        = LoggerMessage.Define<string>(
            LogLevel.Critical,
            1,
            "Critical: {Message}");

    /// <summary>
    /// Executes main fcli logic.
    /// </summary>
    public void Execute(string[] cArgs)
    {
        try
        {
            // Parse command line args into Args object.
            var args = _args.ParseArgs(cArgs);
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
                            "[FCli] Command wasn't constructed properly!");
                    else command.Action();
                }
                catch (InvalidOperationException ex)
                {
                    InvalidOperation(
                        _logger,
                        "[FCli] User tried to invoke unsupported command.",
                        ex);
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
                    CultureInfo.CurrentCulture,
                    _resources.GetLocalizedString("FCli_CriticalError"),
                    ex.GetType().Name,
                    ex.Message));
            Critical(
                _logger,
                "[FCli] An unexpected or critical exception was thrown.",
                ex);
            return;
        }
    }
}