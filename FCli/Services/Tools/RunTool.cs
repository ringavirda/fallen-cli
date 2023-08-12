// FCli namespaces.
using FCli.Exceptions;
using FCli.Models.Dtos;
using FCli.Models.Types;
using FCli.Services.Abstractions;
using static FCli.Models.Args;

namespace FCli.Services.Tools;

/// <summary>
/// A tool that runs given command without saving it. 
/// </summary>
public class RunTool : ToolBase
{
    // DI.
    private readonly IConfig _config;
    private readonly ICommandFactory _factory;

    public RunTool(
        ICommandLineFormatter formatter,
        IResources resources,
        IConfig config,
        ICommandFactory commandFactory)
        : base(formatter, resources)
    {
        _factory = commandFactory;
        _config = config;

        Description = resources.GetLocalizedString("Run_Help");
    }

    // Private data.
    private CommandAlterRequest _runRequest = new();

    // Overrides.
    public override string Name => "Run";
    public override string Description { get; }
    public override List<string> Selectors => new() { "run", "r" };
    public override ToolType Type => ToolType.Run;

    protected override void GuardInit()
    {
        // Guard against no argument.
        if (Arg == string.Empty)
        {
            _formatter.DisplayError(Name, string.Format(
                _resources.GetLocalizedString("FCli_ArgMissing"),
                Name));
            throw new ArgumentException("[Run] No arg was given.");
        }
        // Guard against multiple type flags.
        if (Flags.Select(f => f.Key)
            .Intersect(_config.KnownCommands.Select(c => c.Selector))
            .Count() > 1)
        {
            _formatter.DisplayError(Name, string.Format(
                _resources.GetLocalizedString("FCli_MultipleTypeFlags"),
                Name));
            throw new FlagException(
                "[Run] Attempted to pass multiple command type flags.");
        }
        // Set runner's name.
        _runRequest.Name = "runner";
    }

    protected override void ProcessNextFlag(Flag flag)
    {
        // Specify command line args to run command with.
        if (flag.Key == "options")
        {
            FlagHasValue(flag, Name);
            _runRequest.Options = flag.Value;
        }
        // Parse command and shell type.
        else if (_config.KnownCommands.Any(c => c.Selector == flag.Key))
        {
            var descriptor = _config.KnownCommands
                .First(c => c.Selector == flag.Key);
            // Check if this command a shell one,
            if (descriptor.IsShell)
            {
                // Guard against no shell specified.
                FlagHasValue(flag, Name);
                var shellDescriptor = _config.KnownShells
                    .FirstOrDefault(sh => sh.Selector == flag.Value);
                // Guard against unknown shell.
                if (shellDescriptor != null)
                    _runRequest.Shell = shellDescriptor.Type;
                else
                {
                    _formatter.DisplayError(Name,
                        string.Format(
                            _resources.GetLocalizedString(
                                "FCli_UnknownShell"),
                            string.Join(", ",
                                _config.KnownShells
                                    .Select(sh => sh.Selector)))
                        );
                    throw new ArgumentException(
                        $"[Run] Wasn't able to determine shell type on ({Arg}).");
                }
            }
            // Guard against shell execution.
            else FlagHasNoValue(flag, Name);

            // Set command type.
            _runRequest.Type = descriptor.Type;
        }
        // Throw if flag is unrecognized.
        else UnknownFlag(flag, Name);
    }

    protected override void Action()
    {
        // Guard against no type flag.
        if (_runRequest.Type == CommandType.None)
        {
            _formatter.DisplayError(Name,
                _resources.GetLocalizedString("Run_UnknownCommand"));
            throw new ArgumentException("[Run] Failed to parse given command");
        }
        var command = _factory.Construct(_runRequest);
        // Guard against invalid initialization.
        if (command?.Action != null) command.Action();
        // It is impossible, so if it happens throw it into the root.
        else throw new CriticalException("[Run] Command wasn't initialized.");
    }
}
