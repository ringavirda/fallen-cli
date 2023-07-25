// Vendor namespaces.
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
// FCli namespaces.
using FCli.Models;
using FCli.Exceptions;
using FCli.Services;
using FCli.Services.Format;

namespace FCli;

/// <summary>
/// Main fallen-cli process.
/// </summary>
/// <remarks>
/// Implemented as background service for the app hosting container.
/// </remarks>
public class FallenCli : BackgroundService
{
    // DI.
    private readonly IToolExecutor _executor;
    private readonly ICommandFactory _factory;
    private readonly ILogger<FallenCli> _logger;
    private readonly IHost _host;
    private readonly ICommandLineFormatter _formatter;
    private readonly string[] _args;

    public FallenCli(
        IToolExecutor toolExecutor,
        ICommandFactory commandFactory,
        ILogger<FallenCli> logger,
        IHost host,
        ICommandLineFormatter formatter,
        string[] args)
    {
        _executor = toolExecutor;
        _factory = commandFactory;
        _logger = logger;
        _host = host;
        _formatter = formatter;
        _args = args;
    }
    
    /// <summary>
    /// Executes main fcli logic.
    /// </summary>
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            // Parse command line args into Args object.
            var args = Args.Parse(_args);
            // Handle no args.
            if (args == Args.None)
            {
                _formatter.EchoGreeting();
                return _host.StopAsync(default);
            }
            // Handle --help flag.
            else if (args.Flags.Any(flag => flag.Key == "help")
                && args.Selector == string.Empty)
            {
                _formatter.EchoHelp();
                return _host.StopAsync(default);
            }
            // Handle --version flag.
            else if (args.Flags.Any(flag => flag.Key == "version"))
            {
                _formatter.EchoNameAndVersion();
                return _host.StopAsync(default);
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
                    return _host.StopAsync(default);
                }
            }
            // Execute tool otherwise.
            else _executor.Execute(args, toolType);
            // Stop app process regardless.
            return _host.StopAsync(default);
        }
        // Root exception selector.
        catch (Exception ex)
        {
            _formatter.DisplayError("FCli", $"""
                Something went horribly wrong!
                [{ex.GetType().Name}]: {ex.Message}
                """);
            _logger.LogCritical(ex, "An unexpected exception was thrown.");
            return _host.StopAsync(default);
        }
    }
}
