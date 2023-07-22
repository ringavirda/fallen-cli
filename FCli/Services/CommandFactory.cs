using System.Diagnostics;

using FCli.Services.Data;
using FCli.Models;
using Microsoft.Extensions.Logging;

namespace FCli.Services;

public class CommandFactory
{
    private readonly ICommandLoader _commandLoader;
    private readonly ILogger<CommandFactory> _logger;

    public CommandFactory(
        ICommandLoader commandLoader,
        ILogger<CommandFactory> logger)
    {
        _commandLoader = commandLoader;
        _logger = logger;
    }

    public Command Construct(string name)
    {
        try
        {
            var command = _commandLoader.LoadCommand(name);
            return Construct(
                command.Name,
                command.Path,
                command.Type,
                command.Options);
        }
        catch (ArgumentException ex)
        {
            Console.WriteLine(ex.Message);
            _logger.LogWarning(ex, "");
            throw;
        }
        catch (InvalidOperationException ex)
        {
            Console.WriteLine(ex.Message);
            _logger.LogError(ex, "{message}", ex.Message);
            throw;
        }
        catch (Exception ex)
        {
            var message = "Something went horribly wrong!!!";
            Console.WriteLine(message);
            Console.WriteLine(ex.Message);
            _logger.LogError(ex, "{message}", message);
            throw;
        }
    }

    public static Command Construct(
        string name,
        string path,
        CommandType type,
        string options)
    {
        Action action = type switch
        {
            CommandType.Executable => () =>
            {
                if (Environment.OSVersion.Platform == PlatformID.Unix)
                    Process.Start(path);
                Process.Start(new ProcessStartInfo
                {
                    FileName = path,
                    UseShellExecute = false,
                    Arguments = options,
                    WindowStyle = ProcessWindowStyle.Maximized
                });
            }
            ,
            CommandType.Url => () =>
            {
                if (Environment.OSVersion.Platform == PlatformID.Unix)
                    Process.Start("xdg-open", path);
                else
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = path,
                        UseShellExecute = true
                    });
            }
            ,
            CommandType.CMD => () =>
            {
                if (Environment.OSVersion.Platform == PlatformID.Unix)
                    throw new InvalidOperationException("Powershell is not supported on Linux.");

                Process.Start(new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c {path} {options}",
                    UseShellExecute = false
                });
            }
            ,
            CommandType.Powershell => () =>
            {
                if (Environment.OSVersion.Platform == PlatformID.Unix)
                    throw new InvalidOperationException("Powershell is not supported on Linux.");

                Process.Start(new ProcessStartInfo()
                {
                    FileName = "powershell.exe",
                    Arguments = $"-NoProfile -ExecutionPolicy ByPass -File \"{path}\" -- {options}",
                    UseShellExecute = false
                })?.WaitForExit();
            }
            ,
            CommandType.Bash => () =>
            {
                if (Environment.OSVersion.Platform == PlatformID.Unix)
                    Process.Start("bash", path);
                else
                {
                    path = path.Replace(@"\", @"/");
                    var drive = path.First();
                    path = path.Replace($"{drive}:/", $"/mnt/{char.ToLower(drive)}/");
                    Process.Start(new ProcessStartInfo()
                    {
                        FileName = "powershell.exe",
                        Arguments = $"wsl -e bash {path} {options}",
                        UseShellExecute = false
                    })?.WaitForExit();
                }
            }
            ,
            _ => throw new InvalidOperationException("This should never happen")
        };

        return new Command()
        {
            Name = name,
            Path = path,
            Type = type,
            Action = action,
            Options = options
        };
    }
}
