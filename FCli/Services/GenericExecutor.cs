// Vendor namespaces.
using Microsoft.Extensions.Logging;
// FCli namespaces.
using FCli.Models;
using FCli.Exceptions;
using FCli.Models.Tools;
using FCli.Services.Data;
using FCli.Services.Format;

namespace FCli.Services;

/// <summary>
/// Generic implementation of ToolExecutor.
/// </summary>
public class GenericExecutor : IToolExecutor
{
    // DI.
    private readonly ILogger<GenericExecutor> _logger;

    public GenericExecutor(
        ICommandLoader commandLoader,
        ILogger<GenericExecutor> logger,
        ICommandFactory commandFactory,
        ICommandLineFormatter formatter)
    {
        // Configure tool protos.
        KnownTools = new()
        {
            new AddTool(formatter, this, commandFactory, commandLoader),
            new RemoveTool(formatter, commandLoader),
            new ListTool(formatter, this, commandLoader),
            new RunTool(formatter, this, commandFactory)
        };
        KnownTypeFlags = new() { "script", "url", "exe" };

        _logger = logger;
    }

    public List<string> KnownTypeFlags { get; }
    public List<Tool> KnownTools { get; }
    
    /// <summary>
    /// Execute tool from given type and arg.
    /// </summary>
    /// <param name="args">Tool argument.</param>
    /// <param name="type">Tool type to execute.</param>
    /// <exception cref="CriticalException">If tool selection fails.</exception>
    public void Execute(Args args, ToolType type)
    {
        // Extract tool from the list of known tools.
        var tool = KnownTools
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
        foreach (var tool in KnownTools)
        {
            if (tool.Selectors.Contains(selector))
                return tool.Type;
        }
        // Return None if no match found.
        return ToolType.None;
    }
}
