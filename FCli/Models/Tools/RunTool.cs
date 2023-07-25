// FCli namespaces.
using FCli.Common;
using FCli.Common.Exceptions;
using FCli.Services;
using static FCli.Models.Args;

namespace FCli.Models.Tools;

/// <summary>
/// A tool that runs given command without saving it. 
/// </summary>
public class RunTool : Tool
{
    // From ToolExecutor.
    private readonly IToolExecutor _toolExecutor;
    private readonly ICommandFactory _commandFactory;

    public RunTool(IToolExecutor toolExecutor, ICommandFactory commandFactory)
    {
        _toolExecutor = toolExecutor;
        _commandFactory = commandFactory;
    }

    public override string Name => "Run";
    public override string Description => """
        Run - executes given path or url without saving. Useful for testing.
        Requires path or url, as well as explicit specification of run type
        through a flag.
        Flags:
            --script <shell> - run as script.
            --exe            - run as executable.
            --url            - run as url.
            --help           - show description.
        Usage:
            fcli run c:/awesome --script powershell
            fcli run https://awesome.com --url
        """;
    public override List<string> Selectors => new() { "run", "r" };
    public override ToolType Type => ToolType.Run;
    public override Action<string, List<Flag>> Action =>
        (string arg, List<Flag> flags) =>
        {
            // Handle --help flag.
            if (flags.Any(flag => flag.Key == "help"))
            {
                Helpers.DisplayInfo(Name, Description);
                return;
            }
            // Guard against no argument.
            if (arg == string.Empty)
            {
                Helpers.DisplayWarning(Name, """
                    You need to specify a command to test.
                    To see Run tool usage consult --help.
                    """);
                throw new ArgumentException("Run no type flag was given.");
            }
            // Extract flags.
            var typeFlags = flags
                .Where(flag => _toolExecutor.KnownTypeFlags.Contains(flag.Key));
            var optionsFlag = flags.FirstOrDefault(flag => flag.Key == "options");
            // Guard against no type flag.
            if (!typeFlags.Any())
            {
                Helpers.DisplayWarning(Name, """
                    With Run you need to explicitly specify the type to run.
                    To see all supported type flags use --help.
                    """);
                throw new FlagException("Run no type flag was given.");
            }
            if (typeFlags.Count() != 1)
            {
                Helpers.DisplayWarning(Name, """
                    Run can only have one type defining flag.
                    To see all supported type flags use --help.
                    """);
                throw new FlagException("Run was called with multiple type flags.");
            }
            // Confirm flags.
            var typeFlag = typeFlags.First();
            if (optionsFlag != null)
                FlagHasValue(optionsFlag, Name);
            // Forward declare.
            Command? command = null;
            // Run as shell script.
            if (typeFlag.Key == "script")
            {
                // Script flag has to specify shell type.
                FlagHasValue(typeFlag, Name);
                var type = CommandType.None;
                try
                {
                    type = typeFlag.Value switch
                    {
                        "cmd" => CommandType.CMD,
                        "powershell" => CommandType.Powershell,
                        "bash" => CommandType.Bash,
                        _ => throw new FlagException(
                            $"{typeFlag.Value} - unknown shell.")
                    };
                }
                catch (FlagException)
                {
                    Helpers.DisplayError(Name, $"""
                        Specified shell ({typeFlag.Value}) is not recognized.
                        The only shells that are supported: cmd, powershell, bash. 
                        """);
                    throw;
                }
                // Confirm path.
                var fullPath = ValidatePath(arg, Name);
                // Construct dummy command.
                command = _commandFactory.Construct(
                    "runner", fullPath, type,
                    optionsFlag?.Value ?? string.Empty);

            }
            // Run as executable.
            else if (typeFlag.Key == "exe")
            {
                FlagHasNoValue(typeFlag, Name);
                // Confirm path.
                var fullPath = ValidatePath(arg, Name);
                // Construct dummy command.
                command = _commandFactory.Construct(
                    "runner", fullPath,
                    CommandType.Executable,
                    optionsFlag?.Value ?? string.Empty);
            }
            // Run as website.
            else if (typeFlag.Key == "url")
            {
                FlagHasNoValue(typeFlag, Name);
                // Validate url.
                var uri = ValidateUrl(arg, Name);
                // Construct dummy command.
                command = _commandFactory.Construct(
                    "runner",
                    uri.ToString(),
                    CommandType.Url,
                    string.Empty);
            }
            // Guard against invalid initialization.
            if (command?.Action != null) command.Action();
            // It is impossible, so if it happens throw it into the root.
            else throw new CriticalException("Command wasn't initialized.");
        };
}
