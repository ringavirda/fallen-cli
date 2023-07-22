using FCli.Models;
using FCli.Services.Data;

namespace FCli.Services.Tools;

public class AddProto : Tool, IToolProto
{
    private readonly ToolExecutor _toolExecutor;

    public AddProto(ToolExecutor toolExecutor)
    {
        Name = "Add";
        Description = """
            Add - validates a new command and adds it to the storage.
            Requires a valid path or url as an argument.
            Flags:
                --script <shell> - the path points to the script file.
                --exe            - the path points to the executable.
                --url            - the argument is a url.
                --name           - explicitly specify the name for the command.
                --options        - options to run exe or script with.
                --help           - show description.
            Usage:
                fcli add c:/awesome.exe
                fcli add .\scripts\script --script bash --name sc
            """;
        Type = ToolType.Add;
        Selectors = new() { "add", "a" };

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

            if (arg == string.Empty)
                throw new ArgumentException(
                    "Add tool requires an argument - path or url.");

            if (flags
                .Select(f => f.Key)
                .Intersect(_toolExecutor.KnownTypeFlags)
                .Count() > 1)
                throw new ArgumentException(
                    "Add tool can accept only one of the type flags.");

            var name = string.Empty;
            var type = CommandType.None;
            var options = string.Empty;

            foreach (var flag in flags)
            {
                if (flag.Key == "name")
                {
                    FlagHasValue(flag);

                    name = flag.Value;
                }
                else if (flag.Key == "options")
                {
                    FlagHasValue(flag);

                    options = flag.Value;
                }
                else if (flag.Key == "exe")
                {
                    FlagHasNoValue(flag);

                    arg = ValidatePath(arg);

                    type = CommandType.Executable;
                }
                else if (flag.Key == "url")
                {
                    FlagHasNoValue(flag);

                    ValidateUrl(arg);

                    type = CommandType.Url;
                }
                else if (flag.Key == "script")
                {
                    FlagHasValue(flag);

                    type = flag.Value switch
                    {
                        "cmd" => CommandType.CMD,
                        "powershell" => CommandType.Powershell,
                        "bash" => CommandType.Bash,
                        _ => throw new ArgumentException("""
                            Script flag must also specify type of shell.
                            Supported shells: cmd, powershell, bash.
                            """),
                    };
                }
                else
                    UnknownFlag(flag, "Add");
            }

            if (name == string.Empty || type == CommandType.None)
            {
                if (arg.StartsWith("http://") || arg.StartsWith("https://"))
                {
                    var uri = ValidateUrl(arg);

                    var host = uri.Host.Split('.');

                    if (name == string.Empty)
                        name = host.First() == "www" ? host[1] : host[0];

                    if (type == CommandType.None)
                        type = CommandType.Url;
                }
                else if (arg.Contains('/') || arg.Contains('\\'))
                {
                    arg = ValidatePath(arg);

                    var filename = Path.GetFileName(arg).Split('.');
                    var possibleExtension = filename.Last();

                    if (name == string.Empty)
                        name = filename[0..^1].Aggregate((s1, s2) => $"{s1}{s2}");

                    if (type == CommandType.None)
                    {
                        type = possibleExtension switch
                        {
                            "exe" => CommandType.Executable,
                            "bat" => CommandType.CMD,
                            "ps1" => CommandType.Powershell,
                            "sh" => CommandType.Bash,
                            _ => throw new ArgumentException("""
                                Couldn't recognise the type of file.
                                Please, specify it using flags:
                                    --exe
                                    --script <shell>
                                """)
                        };
                    }
                }
                else
                    throw new ArgumentException("""
                    The type of file wasn't determined.
                    FCli recognizes only file path or url.
                    You can forse execution using type flags.
                    """);
            }

            if (_toolExecutor.ToolProtos.Select(proto => (Tool)proto)
                .Any(tool => tool.Name == name)
                || loader.CommandExists(name))
                throw new ArgumentException($"Name {name} already exists.");

            Console.WriteLine($"""
                Command was parsed:
                name    - {name}
                type    - {type}
                path    - {arg}
                options - {options}
                Saving to storage...
                """);
            var command = CommandFactory.Construct(name, arg, type, options);
            loader.SaveCommand(command);
            Console.WriteLine($"""
                Saved.
                To use the command try:
                    fcli {name}
                """);
        };

        return this;
    }
}