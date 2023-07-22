using FCli.Common.Exceptions;
using FCli.Models;
using FCli.Services.Data;

namespace FCli.Services.Tools;

public class RunProto : Tool, IToolProto
{
    private readonly ToolExecutor _toolExecutor;

    public RunProto(ToolExecutor toolExecutor)
    {
        Name = "Run";
        Description = """
        Run - executes given path or url without saving. Useful for testing.
        Requires path or url, as well as explisit specification of run type
        through a flag.
        Flags:
            --script - run as script.
            --exe    - run as executable.
            --url    - run as url.
            --help   - show description.
        Usage:
            flci run c:/awesome --script powershell
            fcli run https://awesome.com --url
        """;
        Type = ToolType.Run;
        Selectors = new() { "run", "r" };

        _toolExecutor = toolExecutor;
    }
    public Tool GetTool(ICommandLoader loader)
    {
        Action = (string arg, List<Flag> flags) =>
        {
            if (flags.Any(flag => flag.Key == "help"))
            {
                Console.WriteLine(Description);
                return;
            }

            var typeFlags = flags
                .Where(flag => _toolExecutor.KnownTypeFlags.Contains(flag.Key));
            var optionsFlag = flags.FirstOrDefault(flag => flag.Key == "options");

            if (!typeFlags.Any())
                throw new FlagException(
                    "With run you need to expicitly specify the type to run");
            if (typeFlags.Count() != 1)
                throw new FlagException(
                    "Run can only have one type derining flag.");

            var typeFlag = typeFlags.First();
            if (optionsFlag != null)
                FlagHasValue(optionsFlag);

            if (typeFlag.Key == "script")
            {
                FlagHasValue(typeFlag);

                var type = typeFlag.Value switch
                {
                    "cmd" => CommandType.CMD,
                    "powershell" => CommandType.Powershell,
                    "bash" => CommandType.Bash,
                    _ => throw new FlagException(
                        $"{typeFlag.Value} - unknown shell.")
                };

                var fullPath = ValidatePath(arg);

                var commad = CommandFactory.Construct(
                    "runner", fullPath, type,
                    optionsFlag?.Value ?? string.Empty);
                commad.Action();
            }
            else if (typeFlag.Key == "exe")
            {
                FlagHasNoValue(typeFlag);

                var fullPath = ValidatePath(arg);

                var commad = CommandFactory.Construct(
                    "runner", fullPath,
                    CommandType.Executable,
                    optionsFlag?.Value ?? string.Empty);
                commad.Action();
            }
            else if (typeFlag.Key == "url")
            {
                FlagHasNoValue(typeFlag);

                var uri = ValidateUrl(arg);

                var commad = CommandFactory.Construct(
                    "runner",
                    uri.ToString(),
                    CommandType.Url,
                    string.Empty);
                commad.Action();
            }
            else
                UnknownFlag(typeFlag, "Run");
        };
        return this;
    }
}