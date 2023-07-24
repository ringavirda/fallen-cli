// Vendor namespaces.
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
// FCli namespaces.
using FCli.Common;
using FCli.Models;
using FCli.Services;

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
    private readonly IToolExecutor _toolExecutor;
    private readonly ICommandFactory _commandFactory;
    private readonly ILogger<FallenCli> _logger;
    private readonly IHost _host;
    private readonly string[] _args;

    public FallenCli(
        IToolExecutor toolExecutor,
        ICommandFactory commandFactory,
        ILogger<FallenCli> logger,
        IHost host,
        string[] args)
    {
        _toolExecutor = toolExecutor;
        _commandFactory = commandFactory;
        _logger = logger;
        _host = host;
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
                Helpers.EchoGreeting();
                return _host.StopAsync(default);
            }
            // Handle --help flag.
            else if (args.Flags.Any(flag => flag.Key == "help")
                && args.Selector == string.Empty)
            {
                Helpers.EchoHelp();
                return _host.StopAsync(default);
            }
            // Handle --version flag.
            else if (args.Flags.Any(flag => flag.Key == "version"))
            {
                Helpers.EchoNameAndVersion();
                return _host.StopAsync(default);
            }
            // Try parse tool type.
            var toolType = _toolExecutor.ParseType(args);
            // If failed consider it as a command.
            if (toolType == ToolType.None)
            {
                try
                {
                    var command = _commandFactory.Construct(args.Selector);
                    if (command.Action == null)
                        throw new Exception("Command wasn't constructed properly!");
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
            else _toolExecutor.Execute(args, toolType);
            // Stop app process regardless.
            return _host.StopAsync(default);
        }
        // Root exception selector.
        catch (Exception ex)
        {
            Helpers.DisplayError("FCli", $"""
                Something went horribly wrong!
                {ex}
                """);
            _logger.LogCritical(ex, "An unexpected exception was thrown.");
            return _host.StopAsync(default);
        }
    }
}
