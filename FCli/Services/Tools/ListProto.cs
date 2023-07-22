using FCli.Models;
using FCli.Services.Data;

namespace FCli.Services.Tools;

public class ListProto : Tool, IToolProto
{
    public readonly ToolExecutor _toolExecutor;

    public ListProto(ToolExecutor toolExecutor)
    {
        Name = "List";
        Description = """
        List - echos existing commands to the console based on the selection
        given by flags. If no flags given - lists all existing commands.
        Flags:
            --script - adds scripts to listing.
            --exe    - adds executables to listing.
            --url    - adds urls to listing.
            --tools  - lists all available tool selectors.
            --help   - show description.
        Usage:
            fcli list
            fcli list --tools
            fcli ls --script --url 
        """;
        Type = ToolType.List;
        Selectors = new() { "list", "ls" };

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

            var commands = loader.LoadCommands();
            static void EchoCommands(IEnumerable<Command> commands)
            {
                foreach (var command in commands)
                {
                    Console.WriteLine($"[{command.Type}] - {command.Name}:");
                    Console.WriteLine($"\t{command.Path}");
                }
            }

            if (flags.Count == 0)
            {
                Console.WriteLine("No flags given, listing all commands:");
                if (!commands.Any())
                {
                    Console.WriteLine("There are no known commands.");
                    return;
                }
                EchoCommands(commands);
                return;
            }

            foreach (var flag in flags)
            {
                FlagHasNoValue(flag);
                if (flag.Key == "script")
                {
                    Console.WriteLine("Listing all scripts:");
                    var scripts = commands.Where(command =>
                        command.Type == CommandType.CMD
                        || command.Type == CommandType.Powershell
                        || command.Type == CommandType.Bash);
                    if (!scripts.Any())
                    {
                        Console.WriteLine("There are no known scipts.");
                        return;
                    }
                    EchoCommands(scripts);
                }
                else if (flag.Key == "url")
                {
                    Console.WriteLine("Listing all urls...");
                    var urls = commands.Where(command =>
                        command.Type == CommandType.Url);
                    if (!urls.Any())
                    {
                        Console.WriteLine("There are no known urls.");
                        return;
                    }
                    EchoCommands(urls);
                }
                else if (flag.Key == "exe")
                {
                    Console.WriteLine("Listing all executables...");
                    var executables = commands.Where(command =>
                        command.Type == CommandType.Executable);
                    if (!executables.Any())
                    {
                        Console.WriteLine("There are no known executables.");
                        return;
                    }
                    EchoCommands(executables);
                }
                else if (flag.Key == "tool")
                {
                    FlagHasNoValue(flag);

                    var allTools = _toolExecutor.ToolProtos
                        .Select(proto => (Tool)proto)
                            .Select(tool =>
                                $"{tool.Name}: {tool.Selectors.Aggregate((s1, s2)
                                    => $"{s1}, {s2}")}")
                                    .Aggregate((s1, s2) => $"{s1}\n{s2}");

                    Console.WriteLine("All known tool selectors:");
                    Console.WriteLine(allTools);
                }
                else
                    UnknownFlag(flag, "List");
            }
        };

        return this;
    }
}