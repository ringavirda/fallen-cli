using System.Text.RegularExpressions;

namespace FCli.Models;

public partial class Args
{
    public string Selector { get; init; }
    public string Arg { get; init; }
    public List<Flag> Flags { get; init; }

    private Args(string selector, string arg, List<Flag> flags)
    {
        Selector = selector;
        Arg = arg;
        Flags = flags;
    }

    public static Args Parse(string[] args)
    {
        args = SplitArgs(args);
        var buff = args.ToList();
        var flags = new List<Flag>();

        if (args.Length == 0)
            return None;
        else
        {
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i].StartsWith("--"))
                {
                    var flag =
                        i < args.Length - 1 && !args[i + 1].StartsWith("--")
                        ? new Flag(args[i][2..^0], args[i + 1])
                        : new Flag(args[i][2..^0], "");
                    flags.Add(flag);
                    buff.Remove(args[i]);
                    if (flag.Value != "")
                        buff.Remove(flag.Value);
                }
            }
            if (buff.Count > 2)
            {
                var message =
                    "FCli accepts only <tool?> <arg>. There are more then one args.";
                Console.WriteLine(message);
                throw new ArgumentException(message);
            }

            return new Args(
                buff.Count >= 1 ? buff[0] : "",
                buff.Count == 2 ? buff[1] : "",
                flags);
        }
    }

    private static readonly Args _none = new("", "", new());
    public static Args None => _none;

    private static string[] SplitArgs(string[] args)
    {
        var newArgs = new List<string>();
        foreach (var arg in args)
        {
            var match = WithinQuotes().Match(arg);
            if (match.Success)
            {
                newArgs.AddRange(
                    arg.Remove(match.Index, match.Length).Split(" "));
                newArgs.Add(match.Value.Trim(match.Value[0]));
            }
            else if (arg.Contains('\\') || arg.Contains('/'))
                newArgs.Add(arg);
            else
                newArgs.AddRange(arg.Split(" "));
        }
        return newArgs.Where(s => s != string.Empty).ToArray();
    }

    [GeneratedRegex("[\",'].*?[\",']")]
    private static partial Regex WithinQuotes();
}

public record Flag(
    string Key,
    string Value
);