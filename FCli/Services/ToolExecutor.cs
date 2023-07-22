using Microsoft.Extensions.Logging;

using FCli.Common.Exceptions;
using FCli.Models;
using FCli.Services.Data;
using FCli.Services.Tools;

namespace FCli.Services;

public class ToolExecutor
{
    private readonly ICommandLoader _loader;
    private readonly ILogger<ToolExecutor> _logger;

    public ToolExecutor(ICommandLoader loader,
                        ILogger<ToolExecutor> logger)
    {
        _loader = loader;

        ToolProtos = new()
        {
            new AddProto(this),
            new RemoveProto(),
            new ListProto(this),
            new RunProto(this)
        };
        KnownTypeFlags = new() { "script", "url", "exe" };
        _logger = logger;
    }

    public readonly List<string> KnownTypeFlags;
    public readonly List<IToolProto> ToolProtos;

    public void Execute(Args args, ToolType type)
    {
        var proto = ToolProtos.Single(tool => ((Tool)tool).Type == type);
        var tool = proto.GetTool(_loader);
        try
        {
            tool.Action(args.Arg, args.Flags);
        }
        catch (FlagException ex)
        {
            Console.WriteLine(ex.Message);
            Console.WriteLine(
                "Please, consult help page for info about valid flags!");
            _logger.LogWarning(ex, "{message}", ex.Message);
            throw;
        }
        catch (ArgumentException ex)
        {
            Console.WriteLine(ex.Message);
            _logger.LogWarning(ex, "{message}", ex.Message);
            throw;
        }
        catch (Exception ex)
        {
            var message = "Something Went horribly wrong!!!";
            Console.WriteLine(message);
            Console.WriteLine(ex.Message);
            _logger.LogError(ex, "{message}", message);
            throw;
        }
    }

    public ToolType ParseType(Args args)
    {
        if (args.Selector == "") return ToolType.None;

        var selector = args.Selector;
        foreach (var proto in ToolProtos)
        {
            var tool = (Tool)proto;
            if (tool.Selectors.Contains(selector))
                return tool.Type;
        }

        return ToolType.None;
    }
}
