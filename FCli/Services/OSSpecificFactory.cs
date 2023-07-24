// Vendor namespaces.
using Microsoft.Extensions.Logging;
using System.Diagnostics;
// FCli namespaces.
using FCli.Services.Data;
using FCli.Models;
using FCli.Common;

namespace FCli.Services;

/// <summary>
/// Command factory implementation that recognizes user's OS.
/// </summary>
/// <remarks>
/// Supports Windows and Linux operating systems.
/// </remarks>
public class OSSpecificFactory : ICommandFactory
{
    // DI.
    private readonly ICommandLoader _commandLoader;

    public OSSpecificFactory(ICommandLoader commandLoader)
    {
        _commandLoader = commandLoader;
    }

    /// <summary>
    /// Loads command from storage and reconstructs it using OS specific templates.
    /// </summary>
    /// <param name="name">Command selector.</param>
    /// <returns>Command constructed form the storage.</returns>
    /// <exception cref="InvalidOperationException">If given name is unknown.</exception>
    public Command Construct(string name)
    {
        var command = _commandLoader.LoadCommand(name);
        // Guard against unknown command.
        if (command == null)
        {
            Helpers.DisplayError("Command", $"""
                Command ({name}) is not listed amongst stored commands.
                To see all known commands try: fcli list.
                """);
            throw new InvalidOperationException($"Command ({name}) is not a known name.");
        }
        // Return command constructed from the loaded template.
        else return Construct(
            command.Name,
            command.Path,
            command.Type,
            command.Options);
    }

    public Command Construct(
        string name,
        string path,
        CommandType type,
        string options)
    {
        Action action = type switch
        {
            // Parse executable option.
            CommandType.Executable => () =>
            {
                // Linux considers everything as scripts.
                // This option for it is just cosmetic.
                if (Environment.OSVersion.Platform == PlatformID.Unix)
                    Process.Start("bash", path);
                // Windows starts new process for the app and it should be fire
                // from there. No shell execute.
                else Process.Start(new ProcessStartInfo
                {
                    FileName = path,
                    UseShellExecute = false,
                    Arguments = options,
                    WindowStyle = ProcessWindowStyle.Maximized
                });
            }
            ,
            // Parse website option.
            CommandType.Url => () =>
            {
                // Linux uses xdg to open default browser.
                if (Environment.OSVersion.Platform == PlatformID.Unix)
                    Process.Start("xdg-open", path);
                // Windows is smart enough to recognize that it is a link and 
                // by itself open is in a browser. Use shell execute.
                else Process.Start(new ProcessStartInfo
                {
                    FileName = path,
                    UseShellExecute = true
                });
            }
            ,
            // Parse CMD script option.
            CommandType.CMD => () =>
            {
                // Obviously.
                if (Environment.OSVersion.Platform == PlatformID.Unix)
                {
                    Helpers.DisplayError(
                        name,
                        "CMD scripts cannot be run on Linux systems!");
                    throw new InvalidOperationException(
                        $"Attempt to run a CMD script ({path}) on Linux.");
                }
                // Windows starts cmd.exe process without shell.
                Process.Start(new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c {path} {options}",
                    UseShellExecute = false
                });
            }
            ,
            // Parse Powershell script option.
            CommandType.Powershell => () =>
            {
                // Obviously.
                if (Environment.OSVersion.Platform == PlatformID.Unix)
                {
                    Helpers.DisplayError(
                        name,
                        "Powershell scripts cannot be run on Linux systems!");
                    throw new InvalidOperationException(
                        $"[{name}] Attempt to run a Powershell script ({path}) on Linux.");
                }
                // Windows starts powershell.exe process with flags that bypass
                // execution policy and allow for script execution.
                Process.Start(new ProcessStartInfo()
                {
                    FileName = "powershell.exe",
                    Arguments = $"-NoProfile -ExecutionPolicy ByPass -File \"{path}\" -- {options}",
                    UseShellExecute = false
                })?.WaitForExit();
            }
            ,
            // Parse Bash script option.
            CommandType.Bash => () =>
            {
                // Linux executes script using bash shell.
                if (Environment.OSVersion.Platform == PlatformID.Unix)
                    Process.Start("bash", path);
                // Windows uses WSL if it is available to run bash script.
                else
                {
                    // Convert Windows path to WSL path.
                    path = path.Replace(@"\", @"/");
                    var drive = path.First();
                    path = path.Replace($"{drive}:/", $"/mnt/{char.ToLower(drive)}/");
                    // Start bash process in WSL.
                    Process.Start(new ProcessStartInfo()
                    {
                        FileName = "powershell.exe",
                        Arguments = $"wsl -e bash {path} {options}",
                        UseShellExecute = false
                    })?.WaitForExit();
                }
            }
            ,
            // Throws if received unrecognized command type. 
            _ => throw new Exception("Unknown command type was parsed!")
        };
        // Return constructed command.
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
