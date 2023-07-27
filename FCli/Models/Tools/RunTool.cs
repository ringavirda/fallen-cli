// Vendor namespaces.
using System.Resources;
// FCli namespaces.
using FCli.Exceptions;
using FCli.Models.Types;
using FCli.Services;
using FCli.Services.Config;
using FCli.Services.Format;
using static FCli.Models.Args;

namespace FCli.Models.Tools;

/// <summary>
/// A tool that runs given command without saving it. 
/// </summary>
public class RunTool : Tool
{
    // From ToolExecutor.
    private readonly ICommandFactory _factory;
    private readonly IConfig _config;

    public RunTool(
        ICommandLineFormatter formatter,
        ResourceManager manager,
        ICommandFactory commandFactory,
        IConfig config)
        : base(formatter, manager)
    {
        _factory = commandFactory;
        _config = config;

        Description = _resources.GetString("Run_Help")
            ?? formatter.StringNotLoaded();
    }

    public override string Name => "Run";
    public override string Description { get; }
    public override List<string> Selectors => new() { "run", "r" };
    public override ToolType Type => ToolType.Run;
    public override Action<string, List<Flag>> Action =>
        (string arg, List<Flag> flags) =>
        {
            // Handle --help flag.
            if (flags.Any(flag => flag.Key == "help"))
            {
                _formatter.DisplayMessage(Description);
                return;
            }
            // Guard against no argument.
            if (arg == string.Empty)
            {
                _formatter.DisplayError(Name, string.Format(
                    _resources.GetString("FCli_ArgMissing")
                    ?? _formatter.StringNotLoaded(),
                    Name));
                throw new ArgumentException("Run no type flag was given.");
            }
            // Guard against multiple type flags.
            if (flags.Select(f => f.Key)
                .Intersect(_config.KnownCommands.Select(c => c.Selector))
                .Count() > 1)
            {
                _formatter.DisplayError(Name, string.Format(
                    _resources.GetString("FCli_MultipleTypeFlags")
                    ?? _formatter.StringNotLoaded(),
                    Name));
                throw new FlagException(
                    "Attempted to pass multiple command types flags into the Run tool.");
            }
            // Forward declare.
            var type = CommandType.None;
            var shell = ShellType.None;
            var options = string.Empty;
            // Parse f
            foreach (var flag in flags)
            {
                // Specify command line args to run command with.
                if (flag.Key == "options")
                {
                    FlagHasValue(flag, Name);
                    options = flag.Value;
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
                            shell = shellDescriptor.Type;
                        else
                        {
                            _formatter.DisplayError(Name,
                                string.Format(
                                    _resources.GetString("FCli_UnknownShell")
                                    ?? _formatter.StringNotLoaded(),
                                    string.Join(
                                        ", ", 
                                        _config.KnownShells.Select(sh => sh.Selector)))
                                );
                            throw new ArgumentException(
                                $"Wasn't able to determine shell type on ({arg}).");
                        }
                    }
                    // Guard against shell execution.
                    else FlagHasNoValue(flag, Name);

                    // Set command type.
                    type = descriptor.Type;
                }
                // Throw if flag is unrecognized.
                else UnknownFlag(flag, Name);
            }
            // Guard against no type flag.
            if (type == CommandType.None)
            {
                _formatter.DisplayError(Name, 
                    _resources.GetString("Run_UnknownCommand"));
                throw new ArgumentException("Run failed to parse given command");
            }
            var command = _factory.Construct(
                "runner",
                arg,
                type,
                shell,
                options);
            // Guard against invalid initialization.
            if (command?.Action != null) command.Action();
            // It is impossible, so if it happens throw it into the root.
            else throw new CriticalException("Command wasn't initialized.");
        };
}
