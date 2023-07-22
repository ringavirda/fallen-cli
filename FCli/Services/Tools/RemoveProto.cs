using FCli.Models;
using FCli.Services.Data;

namespace FCli.Services.Tools;

public class RemoveProto : Tool, IToolProto
{
    public RemoveProto()
    {
        Name = "Remove";
        Description = """
            Remove - deletes command from storage.
            Flags:
                --yes  - skip confirmation.
                --all  - removes all known commands.
                --help - show description.
            Usage:
                fcli remove awesome --yes
            """;
        Type = ToolType.Remove;
        Selectors = new() { "remove", "rm" };
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

            if (!loader.CommandExists(arg) && !flags.Any(f => f.Key == "all"))
                throw new ArgumentException($"{arg} - is not a command name.");

            bool skipDialog = false;

            foreach (var flag in flags)
            {
                if (flag.Key == "all")
                {
                    FlagHasNoValue(flag);
                    Console.WriteLine("All flag: preparing to delete all known commands.");
                    Console.Write("Are you sure? (yes/any): ");
                    var response = Console.ReadLine();
                    if (response != "yes")
                    {
                        Console.WriteLine("Deletion averted.");
                    }
                    else
                    {
                        Console.WriteLine("Deleting...");
                        var commands = loader.LoadCommands()
                            .Select(c => c.Name).ToList();
                        foreach (var command in commands)
                            loader.DeleteCommand(command);
                        Console.WriteLine("All existing commands have been deleted.");
                    }
                    return;
                }
                if (flag.Key == "yes")
                {
                    FlagHasNoValue(flag);
                    skipDialog = true;
                }
                else
                    UnknownFlag(flag, "Remove");
            }

            Console.WriteLine($"Preparing to delete {arg} command.");
            if (!skipDialog)
            {
                Console.Write("Are you sure? (yes/any): ");
                var response = Console.ReadLine();
                if (response != "yes")
                {
                    Console.WriteLine("Deletion averted.");
                    return;
                }
            }
            Console.WriteLine("Deleting...");
            loader.DeleteCommand(arg);
            Console.WriteLine("Deleted.");
        };

        return this;
    }
}