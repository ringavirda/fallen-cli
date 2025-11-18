using System.Diagnostics;
using System.Globalization;

using FCli.Exceptions;
using FCli.Models;
using FCli.Models.Dtos;
using FCli.Models.Types;
using FCli.Services.Abstractions;

namespace FCli.Services;

/// <summary>
/// Command factory implementation that recognizes user's OS.
/// </summary>
/// <remarks>
/// Supports Windows and Linux operating systems.
/// </remarks>
public class SystemSpecificFactory(
    ICommandLoader commandLoader,
    ICommandLineFormatter formatter,
    IResources resources) : ICommandFactory
{
    // DI.
    private readonly ICommandLoader _loader = commandLoader;
    private readonly ICommandLineFormatter _formatter = formatter;
    private readonly IResources _resources = resources;

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
            _formatter.DisplayError(
                "Command",
                string.Format(
                    CultureInfo.CurrentCulture,
                    _resources.GetLocalizedString("FCli_UnknownName"),
                    name));
            throw new InvalidOperationException(
                $"[Command] {name} - is not a known name.");
        }
        else if (command.Type == CommandType.Group)
            return ConstructGroup(new GroupAlterRequest
            {
                Name = command.Name,
                Sequence = ((Group)command).Sequence
            });
        // Return command constructed from the loaded template.
        else return Construct(new CommandAlterRequest
        {
            Name = command.Name,
            Type = command.Type,
            Shell = command.Shell,
            Path = command.Path,
            Options = command.Options
        });
    }

    public Command Construct(CommandAlterRequest request)
    {
        Action action = request.Type switch
        {
            // Parse Executable option.
            CommandType.Executable => () =>
            {
                // Linux considers everything as scripts.
                // This option for it is just cosmetic.
                if (Environment.OSVersion.Platform == PlatformID.Unix)
                    Process.Start("bash", request.Path);
                // Windows starts new process for the app and it should be fire
                // from there. No shell execute.
                else Process.Start(new ProcessStartInfo
                {
                    FileName = request.Path,
                    UseShellExecute = false,
                    Arguments = request.Path,
                    WindowStyle = ProcessWindowStyle.Maximized
                });
            }
            ,
            // Parse Website option.
            CommandType.Website => () =>
            {
                // Linux uses xdg to open default browser.
                if (Environment.OSVersion.Platform == PlatformID.Unix)
                    Process.Start("xdg-open", request.Path);
                // Windows is smart enough to recognize that it is a link and 
                // by itself open is in a browser. Use shell execute.
                else Process.Start(new ProcessStartInfo
                {
                    FileName = request.Path,
                    UseShellExecute = true
                });
            }
            ,
            // Parse Script option.
            CommandType.Script => () =>
            {
                if (request.Shell == ShellType.Cmd)
                    ConfigureCmd(
                        request.Path,
                        request.Options,
                        false);
                else if (request.Shell == ShellType.Powershell)
                    ConfigurePowershell(
                        request.Path,
                        request.Options,
                        false);
                else if (request.Shell == ShellType.Bash)
                    request.Path = ConfigureBash(
                        request.Path,
                        request.Options,
                        false);
                else if (request.Shell == ShellType.Fish)
                    ConfigureFish(
                        request.Path,
                        request.Options,
                        false);
                else throw new CriticalException(
                    "[Command] Unknown shell type.");
            }
            ,
            CommandType.Directory => () =>
            {
                // If shell is specified.
                if (request.Shell == ShellType.Cmd)
                    ConfigureCmd(request.Path, "", true);
                else if (request.Shell == ShellType.Powershell)
                    ConfigurePowershell(request.Path, "", true);
                else if (request.Shell == ShellType.Bash)
                    ConfigureBash(request.Path, "", true);
                else if (request.Shell == ShellType.Fish)
                    ConfigureFish(request.Path, "", true);
                // This should just work in both Linux and Windows.
                else Process.Start(new ProcessStartInfo()
                {
                    FileName = request.Path,
                    UseShellExecute = true
                });
            }
            ,
            // Throws if received unrecognized command type. 
            _ => throw new CriticalException(
                "[Command] Unknown command type was parsed!")
        };
        // Return constructed command.
        return new Command()
        {
            Name = request.Name,
            Path = request.Path,
            Type = request.Type,
            Shell = request.Shell,
            Action = action,
            Options = request.Options
        };
    }

    /// <summary>
    /// Generates a group of sequentially executed commands.
    /// </summary>
    /// <param name="request">Request model for the desired group.</param>
    /// <returns>Constructed group object.</returns>
    public Group ConstructGroup(GroupAlterRequest request)
    {
        // Setup group logic.
        void action()
        {
            // Execute commands as given.
            foreach (var commandName in request.Sequence)
            {
                var command = Construct(commandName);
                // Guard against bad commands.
                if (command != null && command.Action != null)
                    command.Action();
                else throw new CriticalException(
                    $"[Command] {commandName} - wasn't able to load.");
            }
        }
        return new Group()
        {
            Name = request.Name,
            Path = string.Empty,
            Type = CommandType.Group,
            Shell = ShellType.None,
            Options = string.Empty,
            Action = action,
            Sequence = request.Sequence
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
            _formatter.DisplayError(
                "Command",
                string.Format(
                    CultureInfo.CurrentCulture,
                    _resources.GetLocalizedString("Command_UnsupportedShell"),
                    ShellType.Cmd,
                    PlatformID.Unix));
            throw new InvalidOperationException(
                $"[Command] Attempted to run a CMD script ({path}) on Linux.");
        }
        if (isDirectory) RunAsDirectory("cmd", path, string.Empty);
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
            _formatter.DisplayWarning(
                "Command",
                string.Format(
                    CultureInfo.CurrentCulture,
                    _resources.GetLocalizedString(
                        "Command_UnsupportedShellWarning"),
                    ShellType.Powershell,
                    PlatformID.Unix));
            if (isDirectory) RunAsDirectory("pwsh", path, string.Empty);
            else Process.Start("pwsh", $"{path} {options}").WaitForExit();
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
            else Process.Start("bash", path)
                .WaitForExit();
        }
        // Windows uses WSL if it is available to run bash script.
        else
        {
            _formatter.DisplayWarning(
                "Command",
                string.Format(
                    CultureInfo.CurrentCulture,
                    _resources.GetLocalizedString(
                        "Command_UnsupportedShellWarning"),
                    ShellType.Bash,
                    PlatformID.Win32NT));
            // Convert Windows path to WSL path.
            path = path.Replace(@"\", @"/");
            var drive = path.First();
            path = path.Replace(
                $"{drive}:/",
                $"/mnt/{char.ToLower(drive, CultureInfo.CurrentUICulture)}/");
            // Start bash process in WSL.
            if (asDirectory)
                RunAsDirectory("powershell", path, "wsl");
            else Process.Start(new ProcessStartInfo()
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
            _formatter.DisplayError(
                "Command",
                string.Format(
                    CultureInfo.CurrentCulture,
                    _resources.GetLocalizedString("Command_UnsupportedShell"),
                    ShellType.Fish,
                    PlatformID.Win32NT));
            throw new InvalidOperationException(
                $"[Command] Attempted to run a Fish script ({path}) on Windows.");
        }
        // Linux starts Fish process if it exists.
        if (asDirectory) RunAsDirectory("fish", path, string.Empty);
        else Process.Start(new ProcessStartInfo
        {
            FileName = "fish",
            Arguments = $"{path} {options}",
            UseShellExecute = false
        })?.WaitForExit();
    }

    /// <summary>
    /// Runs given path as a shell directory.
    /// </summary>
    /// <param name="shell">Command line shell type.</param>
    /// <param name="path">Path to the directory.</param>
    /// <param name="options">Additional configs.</param>
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