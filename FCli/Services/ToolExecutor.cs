// Vendor namespaces.
using Microsoft.Extensions.Logging;
// FCli namespaces.
using FCli.Models;
using FCli.Models.Tools;
using FCli.Models.Types;
using FCli.Exceptions;
using FCli.Services.Abstractions;

namespace FCli.Services;

/// <summary>
/// Generic implementation of ToolExecutor.
/// </summary>
public class ToolExecutor : IToolExecutor
{
    // DI.
    private readonly ILogger<ToolExecutor> _logger;

    public ToolExecutor(
        ICommandLoader loader,
        ILogger<ToolExecutor> logger,
        ICommandFactory factory,
        ICommandLineFormatter formatter,
        IConfig config,
        IResources resources)
    {
        // Configure tools.
        Tools = new()
        {
            new AddTool(formatter, resources, config, this, loader, factory),
            new RemoveTool(formatter, resources, loader),
            new ListTool(formatter, resources, config, this, loader),
            new RunTool(formatter, resources, config, factory),
            new ConfigTool(formatter, resources, config),
            new GroupTool(formatter, resources, this, loader, factory),
            new ChangeTool(formatter, resources, config, this, loader, factory)
        };

        _logger = logger;
    }
    
    public List<Tool> Tools { get; }
    
    /// <summary>
    /// Execute tool from given type and arg.
    /// </summary>
    /// <param name="args">Tool argument.</param>
    /// <param name="type">Tool type to execute.</param>
    /// <exception cref="CriticalException">If tool selection fails.</exception>
    public void Execute(Args args, ToolType type)
    {
        // Extract tool from the list of known tools.
        var tool = Tools
            .FirstOrDefault(tool => tool.Type == type) 
            ?? throw new CriticalException("Tool prototype wasn't extracted.");
        // Perform action.
        try
        {
            tool.Action(args.Arg, args.Flags);
        }
        catch (ArgumentException ex)
        {
            // Flag and Arg exceptions are caused by user errors and so have
            // low priority for logging.
            _logger.LogInformation(ex, "Tool argument or flags has failed.");
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
        foreach (var tool in Tools)
        {
            if (tool.Selectors.Contains(selector))
                return tool.Type;
        }
        // Return None if no match found.
        return ToolType.None;
    }
}
