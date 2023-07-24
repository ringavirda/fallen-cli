namespace FCli.Models;

/// <summary>
/// Types that describe command execution.
/// </summary>
public enum CommandType
{
    None,
    Url,
    Executable,
    CMD,
    Powershell,
    Bash
}
