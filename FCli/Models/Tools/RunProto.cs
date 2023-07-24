// FCli namespaces.
using FCli.Common;
using FCli.Common.Exceptions;
using FCli.Services;
using static FCli.Models.Args;

namespace FCli.Models.Tools;

/// <summary>
/// Prototype for the tool that tests given commands.
/// </summary>
public class RunProto : Tool, IToolProto
{
    // From ToolExecutor.
    private readonly IToolExecutor _toolExecutor;
    private readonly ICommandFactory _commandFactory;

    public RunProto(IToolExecutor toolExecutor, ICommandFactory commandFactory)
    {
        Name = "Run";
        Description = """
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
        Type = ToolType.Run;
        Selectors = new() { "run", "r" };

        _toolExecutor = toolExecutor;
        _commandFactory = commandFactory;
    }

    /// <summary>
    /// Construct configured RUN tool from this proto. 
    /// </summary>
    /// <returns>Tool that just runs commands.</returns>
    /// <exception cref="FlagException"></exception>
    public Tool GetTool()
    {
        // Begin RUN logic construction.
        Action = (string arg, List<Flag> flags) =>
        {
            // Handle --help flag.
            if (flags.Any(flag => flag.Key == "help"))
            {
                Helpers.DisplayInfo(Name, Description);
                return;
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
            // Throw if flag is unrecognized.
            else UnknownFlag(typeFlag, Name);
            // Guard against invalid initialization.
            if (command?.Action != null) command.Action();
            // It is impossible, so if it happens throw it into the root.
            else throw new Exception("Command wasn't initialized.");
        };
        // Return constructed RUN tool.
        return this;
    }
}
