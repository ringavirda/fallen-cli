// Vendor namespaces.
using System.Diagnostics;
using System.Resources;
// FCli namespaces.
using FCli.Models;
using FCli.Models.Types;
using FCli.Exceptions;
using FCli.Services.Data;
using FCli.Services.Format;
using System.Runtime.CompilerServices;

namespace FCli.Services;

/// <summary>
/// Command factory implementation that recognizes user's OS.
/// </summary>
/// <remarks>
/// Supports Windows and Linux operating systems.
/// </remarks>
public class SystemSpecificFactory : ICommandFactory
{
    // DI.
    private readonly ICommandLoader _loader;
    private readonly ICommandLineFormatter _formatter;
    private readonly ResourceManager _resources;

    public SystemSpecificFactory(
        ICommandLoader commandLoader,
        ICommandLineFormatter formatter,
        ResourceManager resources)
    {
        _loader = commandLoader;
        _formatter = formatter;
        _resources = resources;
    }

    /// <summary>
    /// Loads command from storage and reconstructs it using OS specific templates.
    /// </summary>
    /// <param name="name">Command selector.</param>
    /// <returns>Command constructed form the storage.</returns>
    /// <exception cref="InvalidOperationException">If given name is unknown.</exception>
    public Command Construct(string name)
    {
        var command = _loader.LoadCommand(name);
        // Guard against unknown command.
        if (command == null)
        {
            _formatter.DisplayError("Command", $"""
                Command ({name}) is not listed amongst stored commands.
                To see all known commands try: fcli list.
                """);
            throw new InvalidOperationException($"Command ({name}) is not a known name.");
        }
        else if (command.Type == CommandType.Group)
            return ConstructGroup(
                command.Name,
                ((Group)command).Sequence);
        // Return command constructed from the loaded template.
        else return Construct(
            command.Name,
            command.Path,
            command.Type,
            command.Shell,
            command.Options);
    }

    public Command Construct(
        string name,
        string path,
        CommandType type,
        ShellType shell,
        string options)
    {
        Action action = type switch
        {
            // Parse Executable option.
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
            // Parse Website option.
            CommandType.Website => () =>
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
            // Parse Script option.
            CommandType.Script => () =>
            {
                if (shell == ShellType.Cmd)
                    ConfigureCmd(path, options, false);
                else if (shell == ShellType.Powershell)
                    ConfigurePowershell(path, options, false);
                else if (shell == ShellType.Bash)
                    path = ConfigureBash(path, options, false);
                else if (shell == ShellType.Fish)
                    ConfigureFish(path, options, false);
                else throw new CriticalException(
                    "CommandFactory received unknown shell type.");
            }
            ,
            CommandType.Directory => () =>
            {
                // If shell is specified.
                if (shell == ShellType.Cmd)
                    ConfigureCmd(path, "", true);
                else if (shell == ShellType.Powershell)
                    ConfigurePowershell(path, "", true);
                else if (shell == ShellType.Bash)
                    ConfigureBash(path, "", true);
                else if (shell == ShellType.Fish)
                    ConfigureFish(path, "", true);
                // This should just work in both Linux and Windows.
                else Process.Start(new ProcessStartInfo()
                {
                    FileName = path,
                    UseShellExecute = true
                });
            }
            ,
            // Throws if received unrecognized command type. 
            _ => throw new CriticalException("Unknown command type was parsed!")
        };
        // Return constructed command.
        return new Command()
        {
            Name = name,
            Path = path,
            Type = type,
            Shell = shell,
            Action = action,
            Options = options
        };
    }

    /// <summary>
    /// Generates a group of sequentially executed commands.
    /// </summary>
    /// <param name="name">Group name.</param>
    /// <param name="commands">Sequence of commands.</param>
    /// <returns>Constructed group object.</returns>
    public Group ConstructGroup(string name, List<string> commands)
    {
        // Setup group logic.
        void action()
        {
            // Execute commands as given.
            foreach (var commandName in commands)
            {
                var command = Construct(commandName);
                // Guard against bad commands.
                if (command != null && command.Action != null)
                    command.Action();
                else throw new CriticalException($"Command ({commandName}) didn't load.");
            }
        }
        return new Group()
        {
            Name = name,
            Path = string.Empty,
            Type = CommandType.Group,
            Shell = ShellType.None,
            Options = string.Empty,
            Action = action,
            Sequence = commands
        };
    }

    /// <summary>
    /// Setup Cmd script/command execution
    /// </summary>
    /// <remarks>
    /// Throws on Linux-like systems.
    /// </remarks>
    /// <param name="path">Path to script/shell command.</param>
    /// <param name="options">Additional args.</param>
    /// <exception cref="InvalidOperationException">If on Linux.</exception>
    private void ConfigureCmd(string path, string options, bool isDirectory)
    {
        // Obviously.
        if (Environment.OSVersion.Platform == PlatformID.Unix)
        {
            _formatter.DisplayError("Command", string.Format(
                _resources.GetString("Command_UnsupportedShell")
                ?? _formatter.StringNotLoaded(),
                ShellType.Cmd, PlatformID.Unix));
            throw new InvalidOperationException(
                $"Attempt to run a CMD script ({path}) on Linux.");
        }
        if (isDirectory)
            RunAsDirectory("cmd", path, string.Empty);
        else
        {
            // Windows starts cmd.exe process without shell.
            Process.Start(new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/c {path} {options}",
                UseShellExecute = false
            })?.WaitForExit();
        }
    }

    /// <summary>
    /// Setups powershell script/command execution.
    /// </summary>
    /// <param name="path">Path to script/shell command.</param>
    /// <param name="options">Additional args.</param>
    /// <param name="options">True if execute as file script.</param>
    private void ConfigurePowershell(string path, string options, bool isDirectory)
    {
        // Try execute on linux.
        if (Environment.OSVersion.Platform == PlatformID.Unix)
        {
            _formatter.DisplayWarning("Command", string.Format(
                _resources.GetString("Command_UnsupportedShellWarning")
                ?? _formatter.StringNotLoaded(),
                ShellType.Powershell, PlatformID.Unix));
            if (isDirectory)
                RunAsDirectory("powershell", path, string.Empty);
            else Process.Start("bash", $"powershell {path} {options}");
        }
        else
        {
            if (isDirectory)
                // Start in same window.
                RunAsDirectory("powershell", path, string.Empty);
            else
            {
                // Windows starts powershell.exe process with flags that bypass
                // execution policy and allow for script execution.
                Process.Start(new ProcessStartInfo()
                {
                    FileName = "powershell.exe",
                    Arguments = $"-NoProfile -ExecutionPolicy ByPass -File \"{path}\" -- {options}",
                    UseShellExecute = false
                })?.WaitForExit();
            }
        }
    }

    /// <summary>
    /// Setups bash script/command execution.
    /// </summary>
    /// <param name="path">Path to script/shell command.</param>
    /// <param name="options">Additional args.</param>
    /// <returns>Path converted to WSL if on Windows.</returns>
    private string ConfigureBash(string path, string options, bool asDirectory)
    {
        // Linux executes script using bash shell.
        if (Environment.OSVersion.Platform == PlatformID.Unix)
        {
            if (asDirectory)
                RunAsDirectory("bash", path, string.Empty);
            Process.Start("bash", path);
        }
        // Windows uses WSL if it is available to run bash script.
        else
        {
            _formatter.DisplayWarning("Command", string.Format(
                _resources.GetString("Command_UnsupportedShellWarning")
                ?? _formatter.StringNotLoaded(),
                ShellType.Bash, PlatformID.Win32NT));
            // Convert Windows path to WSL path.
            path = path.Replace(@"\", @"/");
            var drive = path.First();
            path = path.Replace($"{drive}:/", $"/mnt/{char.ToLower(drive)}/");
            // Start bash process in WSL.
            if (asDirectory)
                RunAsDirectory("powershell", path, "wsl");
            Process.Start(new ProcessStartInfo()
            {
                FileName = "powershell.exe",
                Arguments = $"wsl -e bash {path} {options}",
                UseShellExecute = false
            })?.WaitForExit();
        }

        return path;
    }

    /// <summary>
    /// Setups fish script/command execution.
    /// </summary>
    /// <remarks>
    /// Throws on Windows systems.
    /// </remarks>
    /// <param name="path">Path to script/shell command.</param>
    /// <param name="options">Additional args.</param>
    /// <exception cref="InvalidOperationException">If Windows.</exception>
    private void ConfigureFish(string path, string options, bool asDirectory)
    {
        if (Environment.OSVersion.Platform == PlatformID.Win32NT)
        {
            _formatter.DisplayError("Command", string.Format(
                _resources.GetString("Command_UnsupportedShell")
                ?? _formatter.StringNotLoaded(),
                ShellType.Fish, PlatformID.Win32NT));
            throw new InvalidOperationException(
                $"Attempt to run a Fish script ({path}) on Windows.");
        }
        // Linux starts Fish process if it exists.
        if (asDirectory)
            RunAsDirectory("fish", path, string.Empty);
        Process.Start(new ProcessStartInfo
        {
            FileName = "fish",
            Arguments = $"{path} {options}",
            UseShellExecute = false
        })?.WaitForExit();
    }

    /// <summary>
    /// Runs given path a shell.
    /// </summary>
    /// <param name="shell"></param>
    /// <param name="path"></param>
    /// <param name="options"></param>
    private static void RunAsDirectory(string shell, string path, string options)
    {
        Process.Start(new ProcessStartInfo()
        {
            FileName = shell,
            WorkingDirectory = path,
            Arguments = options,
        })?.WaitForExit();
    }

}