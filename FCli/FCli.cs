using Microsoft.Extensions.Logging;

using FCli.Common;
using FCli.Models;
using FCli.Services;

namespace FCli;

public class FallenCli
{
    private readonly ToolExecutor _toolExecutor;
    private readonly CommandFactory _commandFactory;
    private readonly ILogger<FallenCli> _logger;

    public FallenCli(
        ToolExecutor toolExecutor,
        CommandFactory commandFactory,
        ILogger<FallenCli> logger)
    {
        _toolExecutor = toolExecutor;
        _commandFactory = commandFactory;
        _logger = logger;
    }

    public void Run(string[] args)
    {
        try
        {
            var aargs = Args.Parse(args);

            if (aargs == Args.None)
            {
                Helpers.EchoGreeting();
                return;
            }
            else if (aargs.Flags.Any(flag => flag.Key == "help")
                && aargs.Selector == string.Empty)
            {
                Helpers.EchoHelp();
                return;
            }
            else if (aargs.Flags.Any(flag => flag.Key == "version"))
            {
                Helpers.EchoNameAndVersion();
                return;
            }

            var toolType = _toolExecutor.ParseType(aargs);

            if (toolType == ToolType.None)
            {
                try
                {
                    var command = _commandFactory.Construct(aargs.Selector);
                    command.Action();
                }
                catch (InvalidOperationException ex)
                {
                    Console.WriteLine(ex.Message);
                    _logger.LogWarning(ex, "");
                    throw;
                }
                catch (Exception ex)
                {
                    var message = "Something went horribly wrong!!!";
                    Console.WriteLine(message);
                    Console.WriteLine(ex.Message);
                    _logger.LogError(ex, "{message}", message);
                }
            }
            else
                _toolExecutor.Execute(aargs, toolType);
        }
        catch (Exception)
        {
            return;
        }
    }
}