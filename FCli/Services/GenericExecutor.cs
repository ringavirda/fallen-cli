// Vendor namespaces.
using Microsoft.Extensions.Logging;
// FCli namespaces.
using FCli.Models;
using FCli.Models.Tools;
using FCli.Services.Data;

namespace FCli.Services;

/// <summary>
/// Generic implementation of ToolExecutor.
/// </summary>
/// <remarks>
/// Reconstructs tools from prototypes.
/// </remarks>
public class GenericExecutor : IToolExecutor
{
    // DI.
    private readonly ILogger<GenericExecutor> _logger;

    public GenericExecutor(
        ICommandLoader commandLoader,
        ILogger<GenericExecutor> logger,
        ICommandFactory commandFactory)
    {
        // Configure tool protos.
        ToolProtos = new()
        {
            new AddProto(this, commandFactory, commandLoader),
            new RemoveProto(commandLoader),
            new ListProto(this, commandLoader),
            new RunProto(this, commandFactory)
        };
        KnownTypeFlags = new() { "script", "url", "exe" };

        _logger = logger;
    }

    public List<string> KnownTypeFlags { get; }
    public List<IToolProto> ToolProtos { get; }
    
    /// <summary>
    /// Execute tool from given type and arg.
    /// </summary>
    /// <remarks>
    /// Creates tool from list of known prototypes.
    /// </remarks>
    /// <param name="args">Tool argument.</param>
    /// <param name="type">Tool type to execute.</param>
    /// <exception cref="Exception">If tool selection fails.</exception>
    public void Execute(Args args, ToolType type)
    {
        // Extract tool proto from the list of known tools.
        var proto = ToolProtos
            .FirstOrDefault(tool => ((Tool)tool).Type == type) 
            ?? throw new Exception("Tool prototype wasn't extracted.");
        // Extract tool
        var tool = proto.GetTool();
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
        foreach (var proto in ToolProtos)
        {
            var tool = (Tool)proto;
            if (tool.Selectors.Contains(selector))
                return tool.Type;
        }
        // Return None if no match found.
        return ToolType.None;
    }
}
